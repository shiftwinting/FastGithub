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
    /// github解析器
    /// </summary> 
    sealed class GithubResolver
    {
        private readonly IMemoryCache memoryCache;
        private readonly IOptionsMonitor<FastGithubOptions> options;
        private readonly ILogger<GithubResolver> logger;

        /// <summary>
        /// github解析器
        /// </summary> 
        /// <param name="options"></param>
        public GithubResolver(
            IMemoryCache memoryCache,
            IOptionsMonitor<FastGithubOptions> options,
            ILogger<GithubResolver> logger)
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
            // 缓存，避免做不必要的并发查询
            var key = $"domain:{domain}";
            var address = await this.memoryCache.GetOrCreateAsync(key, async e =>
            {
                e.SetAbsoluteExpiration(TimeSpan.FromMinutes(2d));
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
            this.logger.LogInformation($"[{domain}->{address}]"); 
            return address;
        }
    }
}
