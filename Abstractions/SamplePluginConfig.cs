namespace PluginDemo.Abstractions
{
    public sealed class SamplePluginConfig : PluginConfig
    {
        public SamplePluginConfig(string pluginName, string version, string environment)
            : base(pluginName, version)
        {
            Environment = environment;
        }

        public string Environment { get; }
    }
}
