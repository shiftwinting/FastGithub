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
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.DomainResolve
{
    /// <summary>
    /// 域名解析器
    /// </summary> 
    sealed class DomainResolver : IDomainResolver
    {
        private readonly IMemoryCache domainResolveCache = new MemoryCache(Options.Create(new MemoryCacheOptions()));
        private readonly IMemoryCache disableIPAddressCache = new MemoryCache(Options.Create(new MemoryCacheOptions()));

        private readonly DnscryptProxy dnscryptProxy;
        private readonly FastGithubConfig fastGithubConfig;
        private readonly ILogger<DomainResolver> logger;

        private readonly TimeSpan connectTimeout = TimeSpan.FromSeconds(5d);
        private readonly TimeSpan disableIPExpiration = TimeSpan.FromMinutes(2d);

        private readonly TimeSpan dnscryptExpiration = TimeSpan.FromMinutes(10d);
        private readonly TimeSpan fallbackExpiration = TimeSpan.FromMinutes(2d);
        private readonly TimeSpan loopbackExpiration = TimeSpan.FromSeconds(5d);

        private readonly ConcurrentDictionary<DnsEndPoint, SemaphoreSlim> semaphoreSlims = new();

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
        /// 设置ip不可用
        /// </summary>
        /// <param name="address">ip</param>
        public void SetDisabled(IPAddress address)
        {
            this.disableIPAddressCache.Set(address, address, this.disableIPExpiration);
        }

        /// <summary>
        /// 刷新域名解析结果
        /// </summary>
        /// <param name="domain">域名</param>
        public void FlushDomain(DnsEndPoint domain)
        {
            this.domainResolveCache.Remove(domain);
        }

        /// <summary>
        /// 解析域名
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="OperationCanceledException"></exception>
        /// <exception cref="FastGithubException"></exception>
        /// <returns></returns>
        public async Task<IPAddress> ResolveAsync(DnsEndPoint domain, CancellationToken cancellationToken = default)
        {
            var semaphore = this.semaphoreSlims.GetOrAdd(domain, _ => new SemaphoreSlim(1, 1));
            try
            {
                await semaphore.WaitAsync();
                return await this.ResolveCoreAsync(domain, cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        /// 解析域名
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="OperationCanceledException"></exception>
        /// <exception cref="FastGithubException"></exception>
        /// <returns></returns>
        private async Task<IPAddress> ResolveCoreAsync(DnsEndPoint domain, CancellationToken cancellationToken)
        {
            if (domain.Host == "localhost")
            {
                return IPAddress.Loopback;
            }

            if (this.domainResolveCache.TryGetValue<IPAddress>(domain, out var address) && address != null)
            {
                return address;
            }

            var expiration = this.dnscryptExpiration;
            address = await this.LookupByDnscryptAsync(domain, cancellationToken);

            if (address == null)
            {
                expiration = this.fallbackExpiration;
                address = await this.LookupByFallbackAsync(domain, cancellationToken);
            }

            if (address == null)
            {
                throw new FastGithubException($"当前解析不到{domain.Host}可用的ip，请刷新重试");
            }

            // 往往是被污染的dns
            if (address.Equals(IPAddress.Loopback) == true)
            {
                expiration = this.loopbackExpiration;
            }

            this.domainResolveCache.Set(domain, address, expiration);
            return address;
        }


        /// <summary>
        /// Dnscrypt查找ip
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<IPAddress?> LookupByDnscryptAsync(DnsEndPoint domain, CancellationToken cancellationToken)
        {
            var dns = this.dnscryptProxy.LocalEndPoint;
            if (dns == null)
            {
                return null;
            }

            var dnsClient = new DnsClient(dns);
            var address = await this.LookupAsync(dnsClient, domain, cancellationToken);
            return address ?? await this.LookupAsync(dnsClient, domain, cancellationToken);
        }

        /// <summary>
        /// 回退查找ip
        /// </summary> 
        /// <param name="domain"></param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="OperationCanceledException"></exception>
        /// <returns></returns>        
        private async Task<IPAddress?> LookupByFallbackAsync(DnsEndPoint domain, CancellationToken cancellationToken)
        {
            foreach (var dns in this.fastGithubConfig.FallbackDns)
            {
                var dnsClient = new DnsClient(dns);
                var address = await this.LookupAsync(dnsClient, domain, cancellationToken);
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
        /// <param name="dnsClient"></param>
        /// <param name="domain"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<IPAddress?> LookupAsync(DnsClient dnsClient, DnsEndPoint domain, CancellationToken cancellationToken)
        {
            try
            {
                var addresses = await dnsClient.LookupAsync(domain.Host, cancellationToken);
                var address = await this.FindFastValueAsync(addresses, domain.Port, cancellationToken);

                if (address == null)
                {
                    this.logger.LogWarning($"dns({dnsClient})解析不到{domain.Host}可用的ip解析");
                }
                else
                {
                    this.logger.LogInformation($"dns({dnsClient}): {domain.Host}->{address}");
                }
                return address;
            }
            catch (Exception ex)
            {
                cancellationToken.ThrowIfCancellationRequested();
                this.logger.LogWarning($"dns({dnsClient})无法解析{domain.Host}：{ex.Message}");
                return default;
            }
        }

        /// <summary>
        /// 获取最快的ip
        /// </summary>
        /// <param name="addresses"></param>
        /// <param name="port"></param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="OperationCanceledException"></exception>
        /// <returns></returns>
        private async Task<IPAddress?> FindFastValueAsync(IEnumerable<IPAddress> addresses, int port, CancellationToken cancellationToken)
        {
            addresses = addresses.Where(IsEnableIPAddress).ToArray();
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

            bool IsEnableIPAddress(IPAddress address)
            {
                return this.disableIPAddressCache.TryGetValue(address, out _) == false;
            }
        }


        /// <summary>
        /// 验证远程节点是否可连接
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="OperationCanceledException"></exception>
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
                cancellationToken.ThrowIfCancellationRequested();
                this.SetDisabled(address);
                return default;
            }
            catch (Exception)
            {
                this.SetDisabled(address);
                await Task.Delay(this.connectTimeout, cancellationToken);
                return default;
            }
        }
    }
}
