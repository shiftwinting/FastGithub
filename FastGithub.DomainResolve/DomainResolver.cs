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
        private readonly DnsClient dnsClient;
        private readonly DomainSpeedTestService speedTestService;

        /// <summary>
        /// 域名解析器
        /// </summary>
        /// <param name="dnsClient"></param>
        /// <param name="speedTestService"></param>
        public DomainResolver(
            DnsClient dnsClient,
            DomainSpeedTestService speedTestService)
        {
            this.dnsClient = dnsClient;
            this.speedTestService = speedTestService;
        }

        /// <summary>
        /// 解析ip
        /// </summary>
        /// <param name="domain">域名</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IPAddress> ResolveAsync(string domain, CancellationToken cancellationToken = default)
        {
            await foreach (var address in this.ResolveAllAsync(domain, cancellationToken))
            {
                return address;
            }
            throw new FastGithubException($"解析不到{domain}的IP");
        }

        /// <summary>
        /// 解析域名
        /// </summary>
        /// <param name="domain">域名</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async IAsyncEnumerable<IPAddress> ResolveAllAsync(string domain, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var addresses = this.speedTestService.GetIPAddresses(domain);
            if (addresses.Length > 0)
            {
                foreach (var address in addresses)
                {
                    yield return address;
                }
            }
            else
            {
                this.speedTestService.Add(domain);
                await foreach (var address in this.dnsClient.ResolveAsync(domain, cancellationToken))
                {
                    yield return address;
                }
            }
        }
    }
}
