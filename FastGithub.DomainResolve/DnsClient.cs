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
using System.Net.Sockets;
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

        private readonly TimeSpan minTimeToLive = TimeSpan.FromSeconds(30d);
        private readonly TimeSpan maxTimeToLive = TimeSpan.FromMinutes(10d);

        private readonly int resolveTimeout = (int)TimeSpan.FromSeconds(2d).TotalMilliseconds;
        private static readonly TimeSpan connectTimeout = TimeSpan.FromSeconds(2d);

        private record LookupResult(IPAddress[] Addresses, TimeSpan TimeToLive);

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
        /// <param name="endPoint">远程结节</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async IAsyncEnumerable<IPAddress> ResolveAsync(DnsEndPoint endPoint, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var hashSet = new HashSet<IPAddress>();
            foreach (var dns in this.GetDnsServers())
            {
                var addresses = await this.LookupAsync(dns, endPoint, cancellationToken);
                foreach (var address in addresses)
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
                yield return cryptDns;
            }

            foreach (var fallbackDns in this.fastGithubConfig.FallbackDns)
            {
                if (Socket.OSSupportsIPv6 || fallbackDns.AddressFamily != AddressFamily.InterNetworkV6)
                {
                    yield return fallbackDns;
                }
            }
        }

        /// <summary>
        /// 解析域名
        /// </summary>
        /// <param name="dns"></param>
        /// <param name="endPoint"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<IPAddress[]> LookupAsync(IPEndPoint dns, DnsEndPoint endPoint, CancellationToken cancellationToken = default)
        {
            var key = $"{dns}/{endPoint}";
            var semaphore = this.semaphoreSlims.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync(CancellationToken.None);

            try
            {
                if (this.dnsCache.TryGetValue<IPAddress[]>(key, out var value))
                {
                    return value;
                }

                var result = await this.LookupCoreAsync(dns, endPoint, cancellationToken);
                this.dnsCache.Set(key, result.Addresses, result.TimeToLive);
                return result.Addresses;
            }
            catch (OperationCanceledException)
            {
                this.logger.LogWarning($"dns://{dns}无法解析{endPoint.Host}：请求超时");
                return Array.Empty<IPAddress>();
            }
            catch (Exception ex)
            {
                this.logger.LogWarning($"dns://{dns}无法解析{endPoint.Host}：{ex.Message}");
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
        /// <param name="endPoint"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<LookupResult> LookupCoreAsync(IPEndPoint dns, DnsEndPoint endPoint, CancellationToken cancellationToken = default)
        {
            if (endPoint.Host == LOCALHOST)
            {
                return new LookupResult(new[] { IPAddress.Loopback }, TimeSpan.MaxValue);
            }

            var resolver = dns.Port == DNS_PORT
                ? (IRequestResolver)new TcpRequestResolver(dns)
                : new UdpRequestResolver(dns, new TcpRequestResolver(dns), this.resolveTimeout);

            var addressRecords = await GetAddressRecordsAsync(resolver, endPoint.Host, cancellationToken);
            var addresses = addressRecords
                .Where(item => IPAddress.IsLoopback(item.IPAddress) == false)
                .Select(item => item.IPAddress)
                .ToArray();

            if (addresses.Length == 0)
            {
                return new LookupResult(addresses, this.minTimeToLive);
            }

            if (addresses.Length > 1)
            {
                addresses = await OrderByConnectAnyAsync(addresses, endPoint.Port, cancellationToken);
            }

            var totalTimeToLive = TimeSpan.Zero;
            foreach (var record in addressRecords)
            {
                totalTimeToLive = totalTimeToLive.Add(record.TimeToLive);
            }

            var timeToLive = totalTimeToLive / addressRecords.Count;
            if (timeToLive <= TimeSpan.Zero)
            {
                timeToLive = this.minTimeToLive;
            }
            else if (timeToLive > this.maxTimeToLive)
            {
                timeToLive = this.maxTimeToLive;
            }

            return new LookupResult(addresses, timeToLive);
        }

        /// <summary>
        /// 获取IP记录
        /// </summary>
        /// <param name="resolver"></param>
        /// <param name="domain"></param> 
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private static async Task<IList<IPAddressResourceRecord>> GetAddressRecordsAsync(IRequestResolver resolver, string domain, CancellationToken cancellationToken)
        {
            var addressRecords = new List<IPAddressResourceRecord>();
            if (Socket.OSSupportsIPv4 == true)
            {
                var records = await GetRecordsAsync(RecordType.A);
                addressRecords.AddRange(records);
            }

            if (Socket.OSSupportsIPv6 == true)
            {
                var records = await GetRecordsAsync(RecordType.AAAA);
                addressRecords.AddRange(records);
            }
            return addressRecords;


            async Task<IEnumerable<IPAddressResourceRecord>> GetRecordsAsync(RecordType recordType)
            {
                var request = new Request
                {
                    RecursionDesired = true,
                    OperationCode = OperationCode.Query
                };

                request.Questions.Add(new Question(new Domain(domain), recordType));
                var clientRequest = new ClientRequest(resolver, request);
                var response = await clientRequest.Resolve(cancellationToken);
                return response.AnswerRecords.OfType<IPAddressResourceRecord>();
            }
        }


        /// <summary>
        /// 连接速度排序
        /// </summary>
        /// <param name="addresses"></param>
        /// <param name="port"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private static async Task<IPAddress[]> OrderByConnectAnyAsync(IPAddress[] addresses, int port, CancellationToken cancellationToken)
        {
            var tasks = addresses.Select(address => ConnectAsync(address, port, cancellationToken));
            var fastestAddress = await await Task.WhenAny(tasks);
            if (fastestAddress == null)
            {
                return addresses;
            }

            var list = new List<IPAddress> { fastestAddress };
            foreach (var address in addresses)
            {
                if (address.Equals(fastestAddress) == false)
                {
                    list.Add(address);
                }
            }
            return list.ToArray();
        }

        /// <summary>
        /// 连接指定ip和端口
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private static async Task<IPAddress?> ConnectAsync(IPAddress address, int port, CancellationToken cancellationToken)
        {
            try
            {
                using var timeoutTokenSource = new CancellationTokenSource(connectTimeout);
                using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutTokenSource.Token);
                using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                await socket.ConnectAsync(address, port, linkedTokenSource.Token);
                return address;
            }
            catch (Exception)
            {
                return default;
            }
        }
    }
}
