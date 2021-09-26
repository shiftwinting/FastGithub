using FastGithub.Configuration;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.DomainResolve
{
    /// <summary>
    /// 域名解析器
    /// </summary> 
    sealed class DomainResolver : IDomainResolver
    {
        private readonly DnscryptProxy dnscryptProxy;
        private readonly FastGithubConfig fastGithubConfig;
        private readonly DnsClient dnsClient;

        /// <summary>
        /// 域名解析器
        /// </summary>
        /// <param name="dnscryptProxy"></param>
        /// <param name="fastGithubConfig"></param>
        /// <param name="dnsClient"></param>
        public DomainResolver(
            DnscryptProxy dnscryptProxy,
            FastGithubConfig fastGithubConfig,
            DnsClient dnsClient)
        {
            this.dnscryptProxy = dnscryptProxy;
            this.fastGithubConfig = fastGithubConfig;
            this.dnsClient = dnsClient;
        }

        /// <summary>
        /// 解析ip
        /// </summary>
        /// <param name="domain">域名</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IPAddress?> ResolveAsync(string domain, CancellationToken cancellationToken = default)
        {
            await foreach (var address in this.ResolveAllAsync(domain, cancellationToken))
            {
                return address;
            }
            return default;
        }

        /// <summary>
        /// 解析域名
        /// </summary>
        /// <param name="domain">域名</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async IAsyncEnumerable<IPAddress> ResolveAllAsync(string domain, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var hashSet = new HashSet<IPAddress>();
            foreach (var dns in this.GetDnsServers())
            {
                foreach (var address in await this.dnsClient.LookupAsync(dns, domain, cancellationToken))
                {
                    if (hashSet.Add(address) == true)
                    {
                        yield return address;
                    }
                }
            }
        }

        /// <summary>
        /// 获取dns服务
        /// </summary>
        /// <returns></returns>
        private IEnumerable<IPEndPoint> GetDnsServers()
        {
            var cryptDns = this.dnscryptProxy.LocalEndPoint;
            if (cryptDns != null)
            {
                yield return cryptDns;
            }

            foreach (var fallbackDns in this.fastGithubConfig.FallbackDns)
            {
                yield return fallbackDns;
            }
        }
    }
}
