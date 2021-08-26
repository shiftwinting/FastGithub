using DNS.Client;
using DNS.Protocol;
using FastGithub.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
        private readonly DnscryptProxy dnscryptProxy;
        private readonly ILogger<DomainResolver> logger;

        private readonly TimeSpan lookupTimeout = TimeSpan.FromSeconds(2d);
        private readonly TimeSpan connectTimeout = TimeSpan.FromSeconds(2d);
        private readonly TimeSpan dnscryptExpiration = TimeSpan.FromMinutes(5d);
        private readonly TimeSpan fallbackExpiration = TimeSpan.FromMinutes(1d);
        private readonly TimeSpan loopbackExpiration = TimeSpan.FromSeconds(5d);
        private readonly ConcurrentDictionary<DnsEndPoint, SemaphoreSlim> semaphoreSlims = new();

        /// <summary>
        /// 域名解析器
        /// </summary>
        /// <param name="memoryCache"></param>
        /// <param name="fastGithubConfig"></param>
        /// <param name="dnscryptProxy"></param>
        /// <param name="logger"></param>
        public DomainResolver(
            IMemoryCache memoryCache,
            FastGithubConfig fastGithubConfig,
            DnscryptProxy dnscryptProxy,
            ILogger<DomainResolver> logger)
        {
            this.memoryCache = memoryCache;
            this.fastGithubConfig = fastGithubConfig;
            this.dnscryptProxy = dnscryptProxy;
            this.logger = logger;
        }

        /// <summary>
        /// 解析域名
        /// </summary>
        /// <param name="endPoint"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IPAddress> ResolveAsync(DnsEndPoint endPoint, CancellationToken cancellationToken = default)
        {
            var semaphore = this.semaphoreSlims.GetOrAdd(endPoint, _ => new SemaphoreSlim(1, 1));
            try
            {
                await semaphore.WaitAsync(cancellationToken);
                return await this.LookupAsync(endPoint, cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        /// 查找ip
        /// </summary>
        /// <param name="target"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<IPAddress> LookupAsync(DnsEndPoint target, CancellationToken cancellationToken)
        {
            if (this.memoryCache.TryGetValue<IPAddress>(target, out var address))
            {
                return address;
            }

            var expiration = this.dnscryptExpiration;
            address = await this.LookupCoreAsync(this.dnscryptProxy.EndPoint, target, cancellationToken);

            if (address == null)
            {
                expiration = this.fallbackExpiration;
                address = await this.FallbackLookupAsync(target, cancellationToken);
            }

            if (address == null)
            {
                throw new FastGithubException($"当前解析不到{target.Host}可用的ip，请刷新重试");
            }

            // 往往是被污染的dns
            if (address.Equals(IPAddress.Loopback) == true)
            {
                expiration = this.loopbackExpiration;
            }

            this.logger.LogInformation($"[{target.Host}->{address}]");
            this.memoryCache.Set(target, address, expiration);
            return address;
        }

        /// <summary>
        /// 回退查找ip
        /// </summary>
        /// <param name="target"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<IPAddress?> FallbackLookupAsync(DnsEndPoint target, CancellationToken cancellationToken)
        {
            foreach (var dns in this.fastGithubConfig.FallbackDns)
            {
                var address = await this.LookupCoreAsync(dns, target, cancellationToken);
                if (address != null)
                {
                    return address;
                }
            }
            return default;
        }


        /// <summary>
        /// 查找ip
        /// </summary>
        /// <param name="dns"></param>
        /// <param name="target"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<IPAddress?> LookupCoreAsync(IPEndPoint dns, DnsEndPoint target, CancellationToken cancellationToken)
        {
            try
            {
                var dnsClient = new DnsClient(dns);
                using var timeoutTokenSource = new CancellationTokenSource(this.lookupTimeout);
                using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutTokenSource.Token);
                var addresses = await dnsClient.Lookup(target.Host, RecordType.A, linkedTokenSource.Token);
                return await this.FindFastValueAsync(addresses, target.Port, cancellationToken);
            }
            catch (Exception ex)
            {
                this.logger.LogWarning($"dns({dns})无法解析{target.Host}：{ex.Message}");
                return default;
            }
        }


        /// <summary>
        /// 获取最快的ip
        /// </summary>
        /// <param name="addresses"></param>
        /// <param name="port"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<IPAddress?> FindFastValueAsync(IEnumerable<IPAddress> addresses, int port, CancellationToken cancellationToken)
        {
            if (addresses.Any() == false)
            {
                return default;
            }

            if (port <= 0)
            {
                return addresses.FirstOrDefault();
            }

            var tasks = addresses.Select(address => this.IsAvailableAsync(address, port, cancellationToken));
            var fastTask = await Task.WhenAny(tasks);
            return await fastTask;
        }


        /// <summary>
        /// 验证远程节点是否可连接
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<IPAddress?> IsAvailableAsync(IPAddress address, int port, CancellationToken cancellationToken)
        {
            try
            {
                using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                using var timeoutTokenSource = new CancellationTokenSource(this.connectTimeout);
                using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutTokenSource.Token);
                await socket.ConnectAsync(address, port, linkedTokenSource.Token);
                return address;
            }
            catch (OperationCanceledException)
            {
                return default;
            }
            catch (Exception)
            {
                await Task.Delay(this.connectTimeout, cancellationToken);
                return default;
            }
        }
    }
}
