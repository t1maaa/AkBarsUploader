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
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("You should provide two startup args: Directory and Url");
                Console.ReadKey();
                return;
            }
            
            if (!Directory.Exists(args[0]))
            {
                Console.WriteLine("Incorrect directory");
                Console.ReadKey();
                return;
            }

            using IHost host = CreateHostBuilder(args).Build();
            
            await host.RunAsync();
        }

        static IHostBuilder CreateHostBuilder(string[] args) => Host.CreateDefaultBuilder(args)
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