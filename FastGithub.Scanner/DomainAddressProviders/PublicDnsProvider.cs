using DNS.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace FastGithub.Scanner.DomainAddressProviders
{
    /// <summary>
    /// 公共dns的域名与ip关系提供者
    /// </summary>
    [Service(ServiceLifetime.Singleton, ServiceType = typeof(IDomainAddressProvider))]
    sealed class PublicDnsProvider : IDomainAddressProvider
    {
        private readonly IOptionsMonitor<GithubOptions> options;
        private readonly ILogger<PublicDnsProvider> logger;

        /// <summary>
        /// 获取排序
        /// </summary>
        public int Order => default;

        /// <summary>
        /// 公共dns的域名与ip关系提供者
        /// </summary>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public PublicDnsProvider(
            IOptionsMonitor<GithubOptions> options,
            ILogger<PublicDnsProvider> logger)
        {
            this.options = options;
            this.logger = logger;
        }

        /// <summary>
        /// 创建域名与ip的关系
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<DomainAddress>> CreateDomainAddressesAsync()
        {
            var setting = this.options.CurrentValue.DominAddressProviders.PublicDnsProvider;
            if (setting.Enable == false)
            {
                return Enumerable.Empty<DomainAddress>();
            }

            var result = new HashSet<DomainAddress>();
            foreach (var dns in setting.Dnss)
            {
                var domainAddresses = await this.LookupAsync(dns, setting.Domains);
                foreach (var item in domainAddresses)
                {
                    result.Add(item);
                }
            }

            return result;
        }

        /// <summary>
        /// 反查ip
        /// </summary>
        /// <param name="dns">dns服务器</param>
        /// <param name="domains">域名</param>
        /// <returns></returns>
        private async Task<List<DomainAddress>> LookupAsync(string dns, IEnumerable<string> domains)
        {
            var client = new DnsClient(dns);
            var result = new List<DomainAddress>();

            foreach (var domain in domains)
            {
                try
                {
                    var addresses = await client.Lookup(domain);
                    foreach (var address in addresses)
                    {
                        if (address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            result.Add(new DomainAddress(domain, address));
                        }
                    }
                }
                catch (Exception)
                {
                    this.logger.LogWarning($"dns({dns})无法解析{domain}");
                }
            }

            return result;
        }
    }
}
