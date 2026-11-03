using System;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PluginDemo.Abstractions;

namespace PluginDemo.Plugins
{
    public abstract class PluginBase<TContext> : IPlugin<TContext>
        where TContext : IPluginContext
    {
        private int _initialized;

        // 描述函数在构造时一次性注入，运行期不可变，避免子类误改破坏不变性。
        private readonly Func<TContext, string> _describeFunc;

        protected PluginBase(Func<TContext, string> describeFunc)
        {
            _describeFunc = describeFunc ?? throw new ArgumentNullException(nameof(describeFunc));
        }

        public abstract string Name { get; }

        protected virtual void OnInitialized(TContext context, ILogger logger) { }

        public void Initialize(TContext context)
        {
            var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger(GetType());

            if (Interlocked.CompareExchange(ref _initialized, 1, 0) != 0)
            {
                logger.LogWarning("{Name} already initialized. Skipping.", Name);
                return;
            }

            logger.LogInformation(
                "{Name} initializing. NewConfig: {PluginName} v{Version}, {Detail}",
                Name,
                context.Config.PluginName,
                context.Config.Version,
                _describeFunc(context));

            OnInitialized(context, logger);
        }

        void IPlugin.Initialize(IPluginContext context)
        {
            if (!(context is TContext typed))
            {
                throw new ArgumentException(
                    $"Context type mismatch. Expected {typeof(TContext).Name}, got {context.GetType().Name}.",
                    nameof(context));
            }

            Initialize(typed);
        }
    }
}
