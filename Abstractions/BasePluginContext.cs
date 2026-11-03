using System;

namespace PluginDemo.Abstractions
{
    public class BasePluginContext : IPluginContext<PluginConfig>
    {
        public BasePluginContext(IServiceProvider services, IPluginConfig config, string baseTag)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));

            // 基类只关心基类配置 PluginConfig。当传入的是派生配置（如 SamplePluginConfig）
            // 时，as PluginConfig 会成功（派生即基类），但派生专属字段（如 Environment）
            // 不会在基类层面可见——这是有意为之的"基类只关心基类配置"设计。
            // 当传入的不是 PluginConfig 体系时，回退到一个等价的 PluginConfig。
            _config = config as PluginConfig
                ?? new PluginConfig(config.PluginName, config.Version);

            BaseTag = baseTag ?? throw new ArgumentNullException(nameof(baseTag));
        }

        public IServiceProvider Services { get; }

        private readonly IPluginConfig _config;
        public IPluginConfig Config => _config;

        public string BaseTag { get; }

        PluginConfig IPluginContext<PluginConfig>.NewConfig => (PluginConfig)_config;
    }
}
