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

        public Context(
            ILogger<Context> logger,
            IHostApplicationLifetime appLifetime,
            IConfiguration configuration
            )
        {
            _logger = logger;
            _appLifetime = appLifetime;
            _configuration = configuration;
        }
        
        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (!Directory.Exists(_configuration["SourceDir"]))
            {
                _logger.LogCritical("Incorrect directory");
                return Task.FromException(new ArgumentException("Incorrect directory"));
            }

            if (_configuration["YaDiskOAuthToken"] == null)
            {
                _logger.LogError("No OAuth token.");
                _logger.LogWarning("You can copy-paste it below for one-time usage. Also, you can save it in application.json OR run the app with YaDiskOAuthToken key");
                
                string newToken = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(newToken))
                    return Task.FromException(new ArgumentException("Incorrect OAuth token. Set it in application.json OR run the app with YaDiskOAuthToken key"));
                _configuration["YaDiskOAuthToken"] = newToken;
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}