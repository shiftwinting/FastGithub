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
using System.Threading.Tasks;

namespace FastGithub.Scanner.DomainAddressProviders
{
    /// <summary>
    /// Github公开的域名与ip关系提供者
    /// </summary>
    [Service(ServiceLifetime.Singleton, ServiceType = typeof(IDomainAddressProvider))]
    sealed class GithubMetaProvider : IDomainAddressProvider
    {
        private readonly IOptionsMonitor<GithubOptions> options;
        private readonly HttpClientFactory httpClientFactory;
        private readonly ILogger<GithubMetaProvider> logger;
        private const string META_URI = "https://api.github.com/meta";

        /// <summary>
        /// Github公开的域名与ip关系提供者
        /// </summary>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public GithubMetaProvider(
            IOptionsMonitor<GithubOptions> options,
            HttpClientFactory httpClientFactory,
            ILogger<GithubMetaProvider> logger)
        {
            this.options = options;
            this.httpClientFactory = httpClientFactory;
            this.logger = logger;
        }

        /// <summary>
        /// 创建域名与ip的关系
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<DomainAddress>> CreateDomainAddressesAsync()
        {
            var setting = this.options.CurrentValue.DominAddressProviders.GithubMetaProvider;
            if (setting.Enable == false)
            {
                return Enumerable.Empty<DomainAddress>();
            }

            try
            {
                using var httpClient = this.httpClientFactory.Create();
                var meta = await this.GetMetaAsync(httpClient, setting.MetaUri);
                if (meta != null)
                {
                    return meta.ToDomainAddresses();
                }
            }
            catch (Exception ex)
            {
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
        private async Task<Meta?> GetMetaAsync(HttpClient httpClient, Uri metaUri)
        {
            try
            {
                return await httpClient.GetFromJsonAsync<Meta>(META_URI);
            }
            catch (Exception)
            {
                return await httpClient.GetFromJsonAsync<Meta>(metaUri);
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
            public IEnumerable<DomainAddress> ToDomainAddresses()
            {
                foreach (var range in IPAddressRange.From(this.Web).OrderBy(item => item.Size))
                {
                    if (range.AddressFamily == AddressFamily.InterNetwork)
                    {
                        foreach (var address in range)
                        {
                            yield return new DomainAddress("github.com", address);
                        }
                    }
                }

                foreach (var range in IPAddressRange.From(this.Api).OrderBy(item => item.Size))
                {
                    if (range.AddressFamily == AddressFamily.InterNetwork)
                    {
                        foreach (var address in range)
                        {
                            yield return new DomainAddress("api.github.com", address);
                        }
                    }
                }
            }
        }
    }
}
