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

namespace FastGithub.Scanner.DomainMiddlewares
{
    [Service(ServiceLifetime.Singleton, ServiceType = typeof(IDomainAddressProvider))]
    sealed class RemoteDomainAddressProvider : IDomainAddressProvider
    {
        private readonly IOptionsMonitor<GithubOptions> options;
        private readonly ILogger<RemoteDomainAddressProvider> logger;

        public RemoteDomainAddressProvider(
            IOptionsMonitor<GithubOptions> options,
            ILogger<RemoteDomainAddressProvider> logger)
        {
            this.options = options;
            this.logger = logger;
        }

        public async Task<IEnumerable<DomainAddress>> CreateDomainAddressesAsync()
        {
            var setting = this.options.CurrentValue.RemoteAddressProvider;
            if (setting.Enable == false)
            {
                return Enumerable.Empty<DomainAddress>();
            }

            try
            {
                using var httpClient = new HttpClient();
                var meta = await httpClient.GetFromJsonAsync<Meta>(setting.MetaUri);
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

        private class Meta
        {
            [JsonPropertyName("web")] 
            public string[] Web { get; set; } = Array.Empty<string>();

            [JsonPropertyName("api")]
            public string[] Api { get; set; } = Array.Empty<string>();


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
