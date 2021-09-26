using DNS.Client;
using DNS.Client.RequestResolver;
using DNS.Protocol;
using DNS.Protocol.ResourceRecords;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.DomainResolve
{
    /// <summary>
    /// DNS客户端
    /// </summary>
    sealed class DnsClient
    {
        private readonly ILogger<DnsClient> logger;

        private readonly int resolveTimeout = (int)TimeSpan.FromSeconds(2d).TotalMilliseconds;
        private readonly IMemoryCache dnsCache = new MemoryCache(Options.Create(new MemoryCacheOptions()));
        private readonly TimeSpan dnsExpiration = TimeSpan.FromMinutes(2d);

        /// <summary>
        /// DNS客户端
        /// </summary> 
        /// <param name="logger"></param>
        public DnsClient(ILogger<DnsClient> logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// 解析域名
        /// </summary>
        /// <param name="dns"></param>
        /// <param name="domain"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IPAddress[]> LookupAsync(IPEndPoint dns, string domain, CancellationToken cancellationToken = default)
        {
            var key = $"{dns}:{domain}";
            if (this.dnsCache.TryGetValue<IPAddress[]>(key, out var value))
            {
                return value;
            }

            try
            {
                value = await this.LookupCoreAsync(dns, domain, cancellationToken);
                this.dnsCache.Set(key, value, this.dnsExpiration);

                var items = string.Join(", ", value.Select(item => item.ToString()));
                this.logger.LogInformation($"{dns}：{domain}->[{items}]");
                return value;
            }
            catch (Exception ex)
            {
                this.logger.LogWarning($"{dns}无法解析{domain}：{ex.Message}");
                return Array.Empty<IPAddress>();
            }
        }

        /// <summary>
        /// 解析域名
        /// </summary>
        /// <param name="dns"></param>
        /// <param name="domain"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<IPAddress[]> LookupCoreAsync(IPEndPoint dns, string domain, CancellationToken cancellationToken = default)
        {
            if (domain == "localhost")
            {
                return new[] { IPAddress.Loopback };
            }

            var resolver = dns.Port == 53
                ? (IRequestResolver)new TcpRequestResolver(dns)
                : new UdpRequestResolver(dns, new TcpRequestResolver(dns), this.resolveTimeout);

            var request = new Request
            {
                RecursionDesired = true,
                OperationCode = OperationCode.Query
            };
            request.Questions.Add(new Question(new Domain(domain), RecordType.A));
            var clientRequest = new ClientRequest(resolver, request);
            var response = await clientRequest.Resolve(cancellationToken);
            return response.AnswerRecords.OfType<IPAddressResourceRecord>().Select(item => item.IPAddress).ToArray();
        }
    }
}
