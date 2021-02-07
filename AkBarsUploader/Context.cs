using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AkBarsUploader
{
    internal sealed class Context : IHostedService
    {
        private readonly ILogger _logger;
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly IConfiguration _configuration;
        private readonly IYaDiskExporter _yaDiskExporter;
        public Context(
            ILogger<Context> logger,
            IHostApplicationLifetime appLifetime,
            IConfiguration configuration,
            IYaDiskExporter yaDiskExporter
            )
        {
            _logger = logger;
            _appLifetime = appLifetime;
            _configuration = configuration;
            _yaDiskExporter = yaDiskExporter;
        }
        
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (!_configuration.GetSection("SrcDir").Exists())
            {
                _logger.LogCritical("Use SrcDir= launch option to set source directory.");
                Console.ReadKey();
                _appLifetime.StopApplication();
                return;
            }
            
            if (!Directory.Exists(_configuration["SrcDir"]))
            {
                _logger.LogCritical("Incorrect directory. Check your SrcDir= launch option or in application.json.");
                Console.ReadKey();
                _appLifetime.StopApplication();
                return;
            }

            if (!_configuration.GetSection("DstDir").Exists())
            {
                _logger.LogWarning("Remote directory is not specified. Files will be uploaded to root directory. Press Y to continue and any other key to stop and get additional information");
                if (Console.ReadKey().Key == ConsoleKey.Y)
                {
                    _logger.LogWarning("Use DstDir=\"\" launch option to hide this warning");
                }
                else
                {
                    _logger.LogWarning("Use DstDir= launch option to set name of remote directory");
                    _logger.LogWarning("Use DstDir=\"\" launch option to use root directory and hide warning");
                }
            }

            if (!_configuration.GetSection("YaDiskOAuthToken").Exists() || string.IsNullOrWhiteSpace(_configuration.GetSection("YaDiskOAuthToken").Value))
            {
                _logger.LogError("No OAuth token.");
                _logger.LogWarning("You can copy-paste it below for one-time usage. Also, you can save it in application.json OR run the app with YaDiskOAuthToken launch option");
                
                string newToken = Console.ReadLine();

                if (!string.IsNullOrWhiteSpace(newToken))
                    _configuration["YaDiskOAuthToken"] = newToken;
                else
                {
                    _logger.LogError("Incorrect OAuth token. Application will be stop. Set it in application.json OR run the app with YaDiskOAuthToken launch option");
                    _appLifetime.StopApplication();
                    return;
                }
            }

            _logger.LogInformation("Start exporting...");
            try
            {
                await _yaDiskExporter.RunAsync();
            }
            catch (AggregateException ae)
            {
                foreach (var e in ae.InnerExceptions)
                {
                    _logger.LogError($"{e.GetType().Name}, {e.Data}, {e.Message}, {e.Source}, {e.StackTrace}");
                }
            }
            _appLifetime.StopApplication();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}