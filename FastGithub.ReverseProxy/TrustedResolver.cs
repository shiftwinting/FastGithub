using DNS.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Net;
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

        /// <summary>
        /// 受信任的域名解析器
        /// </summary> 
        /// <param name="options"></param>
        public TrustedResolver(
            IMemoryCache memoryCache,
            IOptionsMonitor<FastGithubOptions> options)
        {
            this.memoryCache = memoryCache;
            this.options = options;
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
            var address = await this.memoryCache.GetOrCreateAsync(key, e =>
            {
                e.SetAbsoluteExpiration(this.cacheTimeSpan);
                return this.LookupAsync(domain, cancellationToken);
            });
            return address;
        }

        /// <summary>
        /// 查找ip
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<IPAddress> LookupAsync(string domain, CancellationToken cancellationToken)
        {
            var endpoint = this.options.CurrentValue.TrustedDns.ToIPEndPoint();
            try
            {
                var dnsClient = new DnsClient(endpoint);
                var addresses = await dnsClient.Lookup(domain, DNS.Protocol.RecordType.A, cancellationToken);
                var address = addresses?.FirstOrDefault();
                if (address == null)
                {
                    throw new Exception($"解析不到{domain}的ip");
                }

                // 如果解析到的ip为本机ip，会产生反向代理请求死循环
                if (address.Equals(IPAddress.Loopback))
                {
                    throw new Exception($"dns受干扰，解析{domain}的ip为{address}");
                }
                return address;
            }
            catch (Exception ex)
            {
                throw new ReverseProxyException($"dns({endpoint})：{ex.Message}", ex);
            }
        }
    }
}
