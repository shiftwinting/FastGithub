using DNS.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.ReverseProxy
{
    /// <summary>
    /// 域名解析器
    /// </summary> 
    sealed class DomainResolver
    {
        private readonly IMemoryCache memoryCache;
        private readonly FastGithubConfig fastGithubConfig;
        private readonly ILogger<DomainResolver> logger;
        private readonly TimeSpan cacheTimeSpan = TimeSpan.FromSeconds(10d);

        /// <summary>
        /// 域名解析器
        /// </summary>
        /// <param name="memoryCache"></param>
        /// <param name="fastGithubConfig"></param>
        /// <param name="logger"></param>
        public DomainResolver(
            IMemoryCache memoryCache,
            FastGithubConfig fastGithubConfig,
            ILogger<DomainResolver> logger)
        {
            this.memoryCache = memoryCache;
            this.fastGithubConfig = fastGithubConfig;
            this.logger = logger;
        }

        /// <summary>
        /// 解析指定的域名
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        /// <exception cref="FastGithubException"></exception>
        public Task<IPAddress> ResolveAsync(string domain, CancellationToken cancellationToken)
        {
            // 缓存以避免做不必要的并发查询
            var key = $"{nameof(DomainResolver)}:{domain}";
            return this.memoryCache.GetOrCreateAsync(key, e =>
            {
                e.SetAbsoluteExpiration(this.cacheTimeSpan);
                return this.LookupAsync(domain, cancellationToken);
            });
        }

        /// <summary>
        /// 查找ip
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="FastGithubException">
        private async Task<IPAddress> LookupAsync(string domain, CancellationToken cancellationToken)
        {
            try
            {
                var dnsClient = new DnsClient(this.fastGithubConfig.PureDns);
                var addresses = await dnsClient.Lookup(domain, DNS.Protocol.RecordType.A, cancellationToken);
                var address = addresses?.FirstOrDefault();
                if (address == null)
                {
                    throw new Exception($"解析不到{domain}的ip");
                }

                // 受干扰的dns，常常返回127.0.0.1来阻断请求
                // 虽然DnscryptProxy的抗干扰能力，但它仍然可能降级到不安全的普通dns上游
                if (address.Equals(IPAddress.Loopback))
                {
                    throw new Exception($"dns被污染，解析{domain}为{address}");
                }

                this.logger.LogInformation($"[{domain}->{address}]");
                return address;
            }
            catch (Exception ex)
            {
                var dns = this.fastGithubConfig.PureDns;
                throw new FastGithubException($"dns({dns})服务器异常：{ex.Message}", ex);
            }
        }
    }
}
