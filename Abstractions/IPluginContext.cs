using System;

namespace PluginDemo.Abstractions
{
    public interface IPluginContext
    {
        IServiceProvider Services { get; }

        IPluginConfig Config { get; }
    }
}
