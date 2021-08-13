using DNS.Client;
using DNS.Protocol;
using FastGithub.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
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
        private readonly IMemoryCache memoryCache;
        private readonly FastGithubConfig fastGithubConfig;
        private readonly ILogger<DomainResolver> logger;
        private readonly TimeSpan cacheTimeSpan = TimeSpan.FromMinutes(1d);

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
            var pureDns = this.fastGithubConfig.PureDns;
            var fastDns = this.fastGithubConfig.FastDns;

            try
            {
                return await LookupCoreAsync(pureDns, domain, cancellationToken);
            }
            catch (Exception)
            {
                this.logger.LogWarning($"由于{pureDns}解析{domain}失败，本次使用{fastDns}");
                return await LookupCoreAsync(fastDns, domain, cancellationToken);
            }
        }

        /// <summary>
        /// 查找ip
        /// </summary>
        /// <param name="dns"></param>
        /// <param name="domain"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<IPAddress> LookupCoreAsync(IPEndPoint dns, string domain, CancellationToken cancellationToken)
        {
            var dnsClient = new DnsClient(dns);
            using var timeoutTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2d));
            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutTokenSource.Token);

            var addresses = await dnsClient.Lookup(domain, RecordType.A, linkedTokenSource.Token);
            var address = addresses?.FirstOrDefault();
            if (address == null)
            {
                throw new FastGithubException($"dns{dns}解析不到{domain}的ip");
            }

            this.logger.LogInformation($"[{domain}->{address}]");
            return address;
        }
    }
}
