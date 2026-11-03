using System;
using PluginDemo.Abstractions;

namespace PluginDemo.Plugins
{
    public class GenericPlugin<TContext> : PluginBase<TContext>
        where TContext : BasePluginContext
    {
        public GenericPlugin() : base(ctx => $"BaseTag: {ctx.BaseTag}")
        {
        }

        public override string Name => "GenericPlugin";
    }
}
