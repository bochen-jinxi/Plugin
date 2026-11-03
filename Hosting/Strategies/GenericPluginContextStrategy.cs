using System;
using PluginDemo.Abstractions;
using PluginDemo.Plugins;

namespace PluginDemo.Hosting.Strategies
{
    // 匹配 GenericPlugin<BasePluginContext> 及其派生类（通过 is 多态判定）。
    public sealed class GenericPluginContextStrategy : PluginContextStrategyBase<GenericPlugin<BasePluginContext>>
    {
        public GenericPluginContextStrategy()
            : base((plugin, services, config) =>
                new BasePluginContext(services, config, $"Base-tag-for-{plugin.Name}"))
        {
        }
    }
}
