using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Scanner
{
    [Service(ServiceLifetime.Singleton)]
    sealed class GithubMetaService
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IOptionsMonitor<GithubOptions> options;
        private readonly ILogger<GithubMetaService> logger;

        public GithubMetaService(
            IHttpClientFactory httpClientFactory,
            IOptionsMonitor<GithubOptions> options,
            ILogger<GithubMetaService> logger)
        {
            this.httpClientFactory = httpClientFactory;
            this.options = options;
            this.logger = logger;
        }

        public async Task<Meta?> GetMetaAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var httpClient = this.httpClientFactory.CreateClient();
                return await httpClient.GetFromJsonAsync<Meta>(this.options.CurrentValue.MetaUri, cancellationToken);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "获取meta.json文件失败");
                return default;
            }
        }
    }
}
