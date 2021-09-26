using FastGithub.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        private readonly DnsClient dnsClient;

        private readonly ConcurrentDictionary<IPEndPoint, SemaphoreSlim> semaphoreSlims = new();
        private readonly IMemoryCache ipEndPointAvailableCache = new MemoryCache(Options.Create(new MemoryCacheOptions()));
        private readonly TimeSpan ipEndPointExpiration = TimeSpan.FromMinutes(2d);
        private readonly TimeSpan ipEndPointConnectTimeout = TimeSpan.FromSeconds(5d);

        /// <summary>
        /// 域名解析器
        /// </summary>
        /// <param name="dnscryptProxy"></param>
        /// <param name="fastGithubConfig"></param>
        /// <param name="dnsClient"></param>
        public DomainResolver(
            DnscryptProxy dnscryptProxy,
            FastGithubConfig fastGithubConfig,
            DnsClient dnsClient)
        {
            this.dnscryptProxy = dnscryptProxy;
            this.fastGithubConfig = fastGithubConfig;
            this.dnsClient = dnsClient;
        }

        /// <summary>
        /// 解析可用的ip
        /// </summary>
        /// <param name="endPoint">远程节点</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IPAddress> ResolveAsync(DnsEndPoint endPoint, CancellationToken cancellationToken = default)
        {
            await foreach (var address in this.ResolveAsync(endPoint.Host, cancellationToken))
            {
                if (await this.IsAvailableAsync(new IPEndPoint(address, endPoint.Port), cancellationToken))
                {
                    return address;
                }
            }
            throw new FastGithubException($"解析不到{endPoint.Host}可用的IP");
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
            var hashSet = new HashSet<IPAddress>();
            foreach (var dns in this.GetDnsServers())
            {
                foreach (var address in await this.dnsClient.LookupAsync(dns, domain, cancellationToken))
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
    }
}
