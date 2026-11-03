using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PluginDemo.Abstractions;
using PluginDemo.Hosting;
using PluginDemo.Hosting.Strategies;
using PluginDemo.Plugins;

namespace PluginDemo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureHostConfiguration(builder =>
                {
                    builder.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        { HostDefaults.EnvironmentKey, Environments.Development }
                    });
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<IPluginConfigFactory, DefaultPluginConfigFactory>();
                    services.AddSingleton<IPluginContextStrategy, GenericPluginContextStrategy>();
                    services.AddSingleton<IPluginContextStrategy, SamplePluginContextStrategy>();
                    services.AddSingleton<IPlugin, GenericPlugin<BasePluginContext>>();
                    services.AddSingleton<IPlugin, SamplePlugin>();
                    services.AddHostedService<PluginHost>();
                
                   
                });
    }
}
