using PluginDemo.Abstractions;

namespace PluginDemo.Plugins
{
    public sealed class SamplePlugin : GenericPlugin<SamplePluginContext>
    {
        public override string Name => "SamplePlugin";
    }
}
