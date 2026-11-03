using System;
using PluginDemo.Abstractions;

namespace PluginDemo.Abstractions
{
    public interface IPluginContextStrategy
    {
        bool CanHandle(IPlugin plugin);
        IPluginContext CreateContext(IPlugin plugin, IServiceProvider services, IPluginConfig config);
    }
}
