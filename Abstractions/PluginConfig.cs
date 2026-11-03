namespace PluginDemo.Abstractions
{
    public class PluginConfig : IPluginConfig
    {
        public PluginConfig(string pluginName, string version)
        {
            PluginName = pluginName;
            Version = version;
        }

        public string PluginName { get; }

        public string Version { get; }
    }
}
