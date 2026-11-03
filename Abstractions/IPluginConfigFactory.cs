namespace PluginDemo.Abstractions
{
    public interface IPluginConfigFactory
    {
        IPluginConfig Create(IPlugin plugin);
    }
}
