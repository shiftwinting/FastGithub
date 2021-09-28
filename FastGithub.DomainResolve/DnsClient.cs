using DNS.Client;
using DNS.Client.RequestResolver;
using DNS.Protocol;
using DNS.Protocol.ResourceRecords;
using FastGithub.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
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

        private readonly DnscryptProxy dnscryptProxy;
        private readonly FastGithubConfig fastGithubConfig;
        private readonly ILogger<DnsClient> logger;

        private readonly ConcurrentDictionary<string, SemaphoreSlim> semaphoreSlims = new();
        private readonly IMemoryCache dnsCache = new MemoryCache(Options.Create(new MemoryCacheOptions()));
        private readonly TimeSpan dnsExpiration = TimeSpan.FromMinutes(2d);
        private readonly int resolveTimeout = (int)TimeSpan.FromSeconds(2d).TotalMilliseconds;

        /// <summary>
        /// DNS客户端
        /// </summary>
        /// <param name="dnscryptProxy"></param>
        /// <param name="fastGithubConfig"></param>
        /// <param name="logger"></param>
        public DnsClient(
            DnscryptProxy dnscryptProxy,
            FastGithubConfig fastGithubConfig,
            ILogger<DnsClient> logger)
        {
            this.dnscryptProxy = dnscryptProxy;
            this.fastGithubConfig = fastGithubConfig;
            this.logger = logger;
        }

        /// <summary>
        /// 解析域名
        /// </summary>
        /// <param name="domain">域名</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async IAsyncEnumerable<IPAddress> ResolveAsync(string domain, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var hashSet = new HashSet<IPAddress>();
            foreach (var dns in this.GetDnsServers())
            {
                foreach (var address in await this.LookupAsync(dns, domain, cancellationToken))
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

        /// <summary>
        /// 解析域名
        /// </summary>
        /// <param name="dns"></param>
        /// <param name="domain"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<IPAddress[]> LookupAsync(IPEndPoint dns, string domain, CancellationToken cancellationToken = default)
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

            return response.AnswerRecords
                .OfType<IPAddressResourceRecord>()
                .Where(item => IPAddress.IsLoopback(item.IPAddress) == false)
                .Select(item => item.IPAddress)
                .ToArray();
        }
    }
}
