using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PluginDemo.Abstractions;

namespace PluginDemo.Hosting
{
    internal sealed class PluginHost : IHostedService
    {
        private readonly IEnumerable<IPluginContextStrategy> _strategies;
        private readonly IServiceProvider _services;
        private readonly IPluginConfigFactory _configFactory;
        private readonly ILogger<PluginHost> _logger;

        public PluginHost(
            IEnumerable<IPluginContextStrategy> strategies,
            IServiceProvider services,
            IPluginConfigFactory configFactory,
            ILogger<PluginHost> logger)
        {
            _strategies = strategies;
            _services = services;
            _configFactory = configFactory;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            foreach (var plugin in _services.GetServices<IPlugin>())
            {
                StartPlugin(plugin);
            }

            return Task.CompletedTask;
        }

        private void StartPlugin(IPlugin plugin)
        {
            try
            {
                var config = _configFactory.Create(plugin);

                // 一个插件只取首个能处理它的策略。这样避免同一插件被多个策略
                // 重复构造 Context 并重复调用 Initialize（虽然 Initialize 幂等，
                // 但多策略匹配本身属于配置错误，应明确为"首个命中"语义）。
                var strategy = _strategies.FirstOrDefault(s => s.CanHandle(plugin));
                if (strategy is null)
                {
                    _logger.LogWarning(
                        "Plugin {Name} has no matching strategy. Skipped.",
                        plugin.Name);
                    return;
                }

                var context = strategy.CreateContext(plugin, _services, config);
                plugin.Initialize(context);

                _logger.LogInformation(
                    "Plugin {Name} started with strategy {Strategy}.",
                    plugin.Name,
                    strategy.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Plugin {Name} failed to start.", plugin.Name);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
