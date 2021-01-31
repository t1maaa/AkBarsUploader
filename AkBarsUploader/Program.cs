using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AkBarsUploader
{
    internal sealed class Program
    {
        static async Task Main(string[] args)
        {
            using IHost host = CreateHostBuilder(args).Build();
            await host.RunAsync();
        }

        static IHostBuilder CreateHostBuilder(string[] args) => Host.CreateDefaultBuilder(args)
            .ConfigureServices((_, services) =>
            {
                services.AddHostedService<Context>();
            })
            .ConfigureAppConfiguration(
                (hostingContext, configuration) =>
                {
                    configuration.Sources.Clear();
                    
                    IHostEnvironment env = hostingContext.HostingEnvironment;

                    if (env.IsDevelopment())
                    {
                        configuration.AddUserSecrets<Program>();
                    }
                    
                    configuration
                        .AddJsonFile("appsettings.json", true, true)
                        .AddCommandLine(args);
                });
    }
}