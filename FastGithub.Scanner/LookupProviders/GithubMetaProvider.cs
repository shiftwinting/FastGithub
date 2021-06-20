using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Scanner.LookupProviders
{
    /// <summary>
    /// Github公开的域名与ip关系提供者
    /// </summary>
    [Service(ServiceLifetime.Singleton, ServiceType = typeof(IGithubLookupProvider))]
    sealed class GithubMetaProvider : IGithubLookupProvider
    {
        private readonly IOptionsMonitor<GithubMetaProviderOptions> options;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly ILogger<GithubMetaProvider> logger;
        private const string META_URI = "https://api.github.com/meta";

        /// <summary>
        /// 获取排序
        /// </summary>
        public int Order => int.MaxValue;

        /// <summary>
        /// Github公开的域名与ip关系提供者
        /// </summary>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public GithubMetaProvider(
            IOptionsMonitor<GithubMetaProviderOptions> options,
            IHttpClientFactory httpClientFactory,
            ILogger<GithubMetaProvider> logger)
        {
            this.options = options;
            this.httpClientFactory = httpClientFactory;
            this.logger = logger;
        }

        /// <summary>
        /// 查找域名与ip关系
        /// </summary>
        /// <param name="domains"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IEnumerable<DomainAddress>> LookupAsync(IEnumerable<string> domains, CancellationToken cancellationToken)
        {
            var setting = this.options.CurrentValue;
            if (setting.Enable == false)
            {
                return Enumerable.Empty<DomainAddress>();
            }

            try
            {
                var httpClient = this.httpClientFactory.CreateClient(nameof(FastGithub));
                var meta = await GetMetaAsync(httpClient, setting.MetaUri, cancellationToken);
                if (meta != null)
                {
                    return meta.ToDomainAddresses(domains);
                }
            }
            catch (Exception ex)
            {
                cancellationToken.ThrowIfCancellationRequested();
                this.logger.LogWarning($"加载远程的ip列表异常：{ex.Message}");
            }

            return Enumerable.Empty<DomainAddress>();
        }


        /// <summary>
        /// 尝试获取meta
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="metaUri"></param>
        /// <returns></returns>
        private async Task<Meta?> GetMetaAsync(HttpClient httpClient, Uri metaUri, CancellationToken cancellationToken)
        {
            try
            {
                return await httpClient.GetFromJsonAsync<Meta>(META_URI, cancellationToken);
            }
            catch (Exception)
            {
                cancellationToken.ThrowIfCancellationRequested();
                this.logger.LogWarning($"当前网络无法从{META_URI}加载github维护的ip数据，{Environment.NewLine}本轮扫描暂时使用{metaUri}的副本数据");
                return await httpClient.GetFromJsonAsync<Meta>(metaUri, cancellationToken);
            }
        }

        /// <summary>
        /// github的meta结构
        /// </summary>
        private class Meta
        {
            [JsonPropertyName("web")]
            public string[] Web { get; set; } = Array.Empty<string>();

            [JsonPropertyName("api")]
            public string[] Api { get; set; } = Array.Empty<string>();

            /// <summary>
            /// 转换为域名与ip关系
            /// </summary>
            /// <returns></returns>
            public IEnumerable<DomainAddress> ToDomainAddresses(IEnumerable<string> domains)
            {
                const string github = "github.com";
                const string apiGithub = "api.github.com";

                if (domains.Contains(github) == true)
                {
                    foreach (var range in IPAddressRange.From(this.Web).OrderBy(item => item.Size))
                    {
                        if (range.AddressFamily == AddressFamily.InterNetwork)
                        {
                            foreach (var address in range)
                            {
                                yield return new DomainAddress(github, address);
                            }
                        }
                    }
                }

                if (domains.Contains(apiGithub) == true)
                {
                    foreach (var range in IPAddressRange.From(this.Api).OrderBy(item => item.Size))
                    {
                        if (range.AddressFamily == AddressFamily.InterNetwork)
                        {
                            foreach (var address in range)
                            {
                                yield return new DomainAddress(apiGithub, address);
                            }
                        }
                    }
                }
            }
        }
    }
}
