using System;
using PluginDemo.Abstractions;

namespace PluginDemo.Abstractions
{
    public interface IPluginContextStrategy<in TPlugin> : IPluginContextStrategy
        where TPlugin : IPlugin
    {
        IPluginContext CreateContext(TPlugin plugin, IServiceProvider services, IPluginConfig config);
    }
}