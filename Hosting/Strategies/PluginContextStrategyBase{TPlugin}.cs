using System;
using PluginDemo.Abstractions;

namespace PluginDemo.Hosting.Strategies
{
    public abstract class PluginContextStrategyBase<TPlugin> :
        IPluginContextStrategy<TPlugin>
        where TPlugin : IPlugin
    {
        // 上下文构造函数在构造时一次性注入，运行期不可变，避免子类误改破坏策略不变性。
        private readonly Func<TPlugin, IServiceProvider, IPluginConfig, IPluginContext> _createContextCore;

        protected PluginContextStrategyBase(
            Func<TPlugin, IServiceProvider, IPluginConfig, IPluginContext> createContextCore)
        {
            _createContextCore = createContextCore
                ?? throw new ArgumentNullException(nameof(createContextCore));
        }

        public bool CanHandle(IPlugin plugin) => plugin is TPlugin;

        public IPluginContext CreateContext(IPlugin plugin, IServiceProvider services, IPluginConfig config)
        {
            if (plugin is TPlugin typedPlugin)
            {
                return _createContextCore(typedPlugin, services, config);
            }
            throw new ArgumentException(
                $"Plugin type mismatch. Expected {typeof(TPlugin).Name}, got {plugin.GetType().Name}.",
                nameof(plugin));
        }

        public IPluginContext CreateContext(TPlugin plugin, IServiceProvider services, IPluginConfig config)
        {
            return _createContextCore(plugin, services, config);
        }
    }
}
