using FastGithub.Configuration;
using System.Collections.Generic;
using System.Net;
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

        /// <summary>
        /// 域名解析器
        /// </summary> 
        /// <param name="dnsClient"></param>
        public DomainResolver(DnsClient dnsClient)
        {
            this.dnsClient = dnsClient;
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
        public IAsyncEnumerable<IPAddress> ResolveAllAsync(string domain, CancellationToken cancellationToken)
        {
            return this.dnsClient.ResolveAsync(domain, cancellationToken);
        }
    }
}
