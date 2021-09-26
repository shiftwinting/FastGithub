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
    /// 域名解析器
    /// </summary> 
    sealed class DomainResolver : IDomainResolver
    {
        private readonly DnscryptProxy dnscryptProxy;
        private readonly FastGithubConfig fastGithubConfig;
        private readonly ILogger<DomainResolver> logger;

        private readonly ConcurrentDictionary<IPEndPoint, SemaphoreSlim> semaphoreSlims = new();
        private readonly IMemoryCache ipEndPointAvailableCache = new MemoryCache(Options.Create(new MemoryCacheOptions()));
        private readonly TimeSpan ipEndPointExpiration = TimeSpan.FromMinutes(2d);
        private readonly TimeSpan ipEndPointConnectTimeout = TimeSpan.FromSeconds(5d);

        /// <summary>
        /// 域名解析器
        /// </summary>
        /// <param name="dnscryptProxy"></param>
        /// <param name="fastGithubConfig"></param>
        /// <param name="logger"></param>
        public DomainResolver(
            DnscryptProxy dnscryptProxy,
            FastGithubConfig fastGithubConfig,
            ILogger<DomainResolver> logger)
        {
            this.dnscryptProxy = dnscryptProxy;
            this.fastGithubConfig = fastGithubConfig;
            this.logger = logger;
        }

        /// <summary>
        /// 解析域名
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IPAddress> ResolveAsync(DnsEndPoint domain, CancellationToken cancellationToken)
        {
            await foreach (var address in this.ResolveAsync(domain.Host, cancellationToken))
            {
                if (await this.IsAvailableAsync(new IPEndPoint(address, domain.Port), cancellationToken))
                {
                    return address;
                }
            }
            throw new FastGithubException($"解析不到{domain.Host}可用的IP");
        }

        /// <summary>
        /// 验证远程节点是否可连接
        /// </summary>
        /// <param name="ipEndPoint"></param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="OperationCanceledException"></exception>
        /// <returns></returns>
        private async Task<bool> IsAvailableAsync(IPEndPoint ipEndPoint, CancellationToken cancellationToken)
        {
            var semaphore = this.semaphoreSlims.GetOrAdd(ipEndPoint, _ => new SemaphoreSlim(1, 1));
            try
            {
                await semaphore.WaitAsync(CancellationToken.None);
                if (this.ipEndPointAvailableCache.TryGetValue<bool>(ipEndPoint, out var available))
                {
                    return available;
                }

                try
                {
                    using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                    using var timeoutTokenSource = new CancellationTokenSource(this.ipEndPointConnectTimeout);
                    using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutTokenSource.Token);
                    await socket.ConnectAsync(ipEndPoint, linkedTokenSource.Token);
                    available = true;
                }
                catch (Exception)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    available = false;
                }

                this.ipEndPointAvailableCache.Set(ipEndPoint, available, ipEndPointExpiration);
                return available;
            }
            finally
            {
                semaphore.Release();
            }
        }


        /// <summary>
        /// 解析域名
        /// </summary>
        /// <param name="domain">域名</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async IAsyncEnumerable<IPAddress> ResolveAsync(string domain, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (domain == "localhost")
            {
                yield return IPAddress.Loopback;
                yield break;
            }

            var hashSet = new HashSet<IPAddress>();
            var cryptDns = this.dnscryptProxy.LocalEndPoint;
            if (cryptDns != null)
            {
                var dnsClient = new DnsClient(cryptDns);
                foreach (var address in await this.LookupAsync(dnsClient, domain, cancellationToken))
                {
                    if (hashSet.Add(address) == true)
                    {
                        yield return address;
                    }
                }
            }

            foreach (var fallbackDns in this.fastGithubConfig.FallbackDns)
            {
                var dnsClient = new DnsClient(fallbackDns);
                foreach (var address in await this.LookupAsync(dnsClient, domain, cancellationToken))
                {
                    if (hashSet.Add(address) == true)
                    {
                        yield return address;
                    }
                }
            }
        }

        /// <summary>
        /// 查找ip
        /// </summary>
        /// <param name="dnsClient"></param>
        /// <param name="domain"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<IPAddress[]> LookupAsync(DnsClient dnsClient, string domain, CancellationToken cancellationToken)
        {
            try
            {
                var addresses = await dnsClient.LookupAsync(domain, cancellationToken);
                var items = string.Join(", ", addresses.Select(item => item.ToString()));
                this.logger.LogInformation($"{dnsClient}：{domain}->[{items}]");
                return addresses;
            }
            catch (Exception ex)
            {
                cancellationToken.ThrowIfCancellationRequested();
                this.logger.LogWarning($"{dnsClient}无法解析{domain}：{ex.Message}");
                return Array.Empty<IPAddress>();
            }
        }
    }
}
