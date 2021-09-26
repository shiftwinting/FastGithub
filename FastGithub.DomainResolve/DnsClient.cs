using DNS.Client;
using DNS.Client.RequestResolver;
using DNS.Protocol;
using DNS.Protocol.ResourceRecords;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
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
        private const int DNS_PORT = 53;
        private const string LOCALHOST = "localhost";
        private readonly ILogger<DnsClient> logger;

        private readonly ConcurrentDictionary<string, SemaphoreSlim> semaphoreSlims = new();
        private readonly IMemoryCache dnsCache = new MemoryCache(Options.Create(new MemoryCacheOptions()));
        private readonly TimeSpan dnsExpiration = TimeSpan.FromMinutes(2d);
        private readonly int resolveTimeout = (int)TimeSpan.FromSeconds(2d).TotalMilliseconds;

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
            var semaphore = this.semaphoreSlims.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync(CancellationToken.None);

            try
            {
                if (this.dnsCache.TryGetValue<IPAddress[]>(key, out var value) == false)
                {
                    value = await this.LookupCoreAsync(dns, domain, cancellationToken);
                    this.dnsCache.Set(key, value, this.dnsExpiration);

                    var items = string.Join(", ", value.Select(item => item.ToString()));
                    this.logger.LogInformation($"dns://{dns}：{domain}->[{items}]");
                }
                return value;
            }
            catch (Exception ex)
            {
                this.logger.LogWarning($"dns://{dns}无法解析{domain}：{ex.Message}");
                return Array.Empty<IPAddress>();
            }
            finally
            {
                semaphore.Release();
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
            if (domain == LOCALHOST)
            {
                return new[] { IPAddress.Loopback };
            }

            var resolver = dns.Port == DNS_PORT
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
