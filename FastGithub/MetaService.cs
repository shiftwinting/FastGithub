using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace FastGithub
{
    sealed class MetaService
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IOptionsMonitor<GithubOptions> options;
        private readonly ILogger<MetaService> logger;

        public MetaService(
            IHttpClientFactory httpClientFactory,
            IOptionsMonitor<GithubOptions> options,
            ILogger<MetaService> logger)
        {
            this.httpClientFactory = httpClientFactory;
            this.options = options;
            this.logger = logger;
        }

        public async Task<Meta?> GetMetaAsync()
        {
            try
            {
                var httpClient = this.httpClientFactory.CreateClient();
                return await httpClient.GetFromJsonAsync<Meta>(this.options.CurrentValue.MetaUri);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "获取meta.json文件失败");
                return default;
            }
        }
    }
}
