using System;

namespace PluginDemo.Abstractions
{
    public sealed class SamplePluginContext : BasePluginContext, IPluginContext<SamplePluginConfig>
    {
        internal SamplePluginContext(IServiceProvider services, SamplePluginConfig config, string baseTag)
            : base(services, config, baseTag)
        {
        }

        SamplePluginConfig IPluginContext<SamplePluginConfig>.NewConfig => (SamplePluginConfig)Config;
    }
}
