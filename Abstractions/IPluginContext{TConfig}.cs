namespace PluginDemo.Abstractions
{
    public interface IPluginContext<out TConfig> : IPluginContext
        where TConfig : IPluginConfig
    {
          TConfig NewConfig { get; }
    }
}
