using System;
using PluginDemo.Abstractions;

namespace PluginDemo.Hosting
{
    internal sealed class DefaultPluginConfigFactory : IPluginConfigFactory
    {
        private const string DefaultVersion = "1.0.0";

        private readonly Func<IPlugin, IPluginConfig> _create =
            p => new PluginConfig(p.Name, DefaultVersion);

        public IPluginConfig Create(IPlugin plugin) => _create(plugin);
    }
}
