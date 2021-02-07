using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AkBarsUploader
{
    public class YaDiskExporter : IYaDiskExporter
    {
        private static readonly string _baseUrl = "https://cloud-api.yandex.net/v1/disk/resources?path=";
        private readonly string _token;
        private readonly  string _dstDir;
        private readonly string _srcDir;
        private readonly string _overwrite;
        private readonly HttpClient _client;
        private readonly ILogger<YaDiskExporter> _logger;
        private readonly IConfiguration _configuration;
        
        public YaDiskExporter(ILogger<YaDiskExporter> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _srcDir = _configuration["SrcDir"];
            _dstDir = _configuration["DstDir"];
            _token = string.Concat("OAuth ", configuration["YaDiskOAuthToken"]);
            _overwrite = _configuration.GetSection("Overwrite").Exists() ? _configuration["Overwrite"].ToLowerInvariant() : "false";
            _client = new HttpClient
            {
                BaseAddress = new Uri(string.Concat(_baseUrl, _dstDir)),
                DefaultRequestHeaders =
                {
                    Authorization = AuthenticationHeaderValue.Parse(_token),
                    Accept = { MediaTypeWithQualityHeaderValue.Parse("application/json") },
                }
            };
        }

        public async Task RunAsync()
        {
            var filesList = Directory.GetFiles(_srcDir);
            if (filesList.Length == 0)
            {
                _logger.LogInformation("No files in directory.");
                return;
            };
            
            if ((await _client.GetAsync(_client.BaseAddress)).StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogInformation("Remote directory is not exist. Creating...");
                
               var response = await _client.PutAsync(_client.BaseAddress, null);
               
               _logger.LogInformation(response.IsSuccessStatusCode
                   ? "Remote folder created successfully."
                   : $"Directory creation failed with http code {response.StatusCode} and message { (await response.Content.ReadFromJsonAsync<YaDiskResponse>())?.Description}");
            }

            var tasks = new List<Task>();

            foreach (var file in filesList)
            {
                _logger.LogInformation($"Uploading {file}...");
                tasks.Add(Task.Run(() => UploadFile(Path.GetFileName(file))).ContinueWith(async task =>
                {
                    if (task.Result.Value.IsSuccessStatusCode)
                    {
                        _logger.LogInformation($"File {task.Result.Key} uploaded successfully.");
                    }
                    else
                    {
                        _logger.LogWarning(task.Result.Value.StatusCode == HttpStatusCode.Forbidden
                            ? $"Remote file {task.Result.Key} already exists. If you want to overwrite existed files use Overwrite=true launch option."
                            : $"Uploading file {task.Result.Key} failed with http code {task.Result.Value.StatusCode} and message { (await task.Result.Value.Content.ReadFromJsonAsync<YaDiskResponse>())?.Message}");
                    }

                }, TaskContinuationOptions.OnlyOnRanToCompletion));
            }
            try
            {
                Task.WaitAll(tasks.ToArray());
            }
            catch (AggregateException ae)
            {
                throw ae.Flatten();
            }
        }

        private async Task<KeyValuePair<string, HttpResponseMessage>> UploadFile(string filename)
        {
            using HttpClient client = new HttpClient
            {
                BaseAddress = new Uri($"https://cloud-api.yandex.net/v1/disk/resources/upload?path={_dstDir}/{filename}&overwrite={_overwrite}"),
                DefaultRequestHeaders =
                {
                    Authorization = AuthenticationHeaderValue.Parse(_token),
                    Accept = {MediaTypeWithQualityHeaderValue.Parse("application/json")},
                }
            };
            var getResponse = await client.GetAsync(client.BaseAddress);
            var getResponseBody =  await getResponse.Content.ReadFromJsonAsync<YaDiskResponse>();
            var streamContent = new StreamContent(File.OpenRead($"{_srcDir}\\{filename}"));
            var putResponse = await client.PutAsync(getResponseBody?.Href, streamContent);
            return new KeyValuePair<string, HttpResponseMessage>(filename, putResponse);
        }
    }
}