using System;
using Microsoft.Extensions.Hosting;
using PluginDemo.Abstractions;
using PluginDemo.Plugins;

namespace PluginDemo.Hosting.Strategies
{
    // 匹配 GenericPlugin<SamplePluginContext> 及其派生类（如 SamplePlugin）。
    public sealed class SamplePluginContextStrategy : PluginContextStrategyBase<GenericPlugin<SamplePluginContext>>
    {
        public SamplePluginContextStrategy(IHostEnvironment env)
            : base((plugin, services, config) =>
            {
                var sampleConfig = config as SamplePluginConfig
                    ?? new SamplePluginConfig(config.PluginName, config.Version, env.EnvironmentName);

                return new SamplePluginContext(services, sampleConfig, $"Sample-tag-for-{plugin.Name}");
            })
        {
            _ = env ?? throw new ArgumentNullException(nameof(env));
        }
    }
}
