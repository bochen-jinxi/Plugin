namespace PluginDemo.Abstractions
{
    public interface IPlugin<in TContext> : IPlugin where TContext : IPluginContext
    {
        void Initialize(TContext context);
    }
}
