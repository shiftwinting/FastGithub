using DNS.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.ReverseProxy
{
    /// <summary>
    /// 受信任的域名解析器
    /// </summary> 
    sealed class TrustedResolver
    {
        private readonly IMemoryCache memoryCache;
        private readonly TimeSpan cacheTimeSpan = TimeSpan.FromSeconds(10d);
        private readonly IOptionsMonitor<FastGithubOptions> options;
        private readonly ILogger<TrustedResolver> logger;

        /// <summary>
        /// 受信任的域名解析器
        /// </summary> 
        /// <param name="options"></param>
        public TrustedResolver(
            IMemoryCache memoryCache,
            IOptionsMonitor<FastGithubOptions> options,
            ILogger<TrustedResolver> logger)
        {
            this.memoryCache = memoryCache;
            this.options = options;
            this.logger = logger;
        }

        /// <summary>
        /// 解析指定的域名
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        public async Task<IPAddress> ResolveAsync(string domain, CancellationToken cancellationToken)
        {
            // 缓存以避免做不必要的并发查询
            var key = $"domain:{domain}";
            var address = await this.memoryCache.GetOrCreateAsync(key, async e =>
            {
                e.SetAbsoluteExpiration(this.cacheTimeSpan);
                var dnsClient = new DnsClient(this.options.CurrentValue.TrustedDns.ToIPEndPoint());
                var addresses = await dnsClient.Lookup(domain, DNS.Protocol.RecordType.A, cancellationToken);
                return addresses?.FirstOrDefault();
            });

            if (address == null)
            {
                var message = $"无法解析{domain}的ip";
                this.logger.LogWarning(message);
                throw new HttpRequestException(message);
            }

            this.logger.LogInformation($"[{address}->{domain}]");
            return address;
        }
    }
}
