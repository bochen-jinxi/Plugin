namespace PluginDemo.Abstractions
{
    public interface IPlugin
    {
        string Name { get; }

        void Initialize(IPluginContext context);
    }
}
