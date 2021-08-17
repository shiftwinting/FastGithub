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
        private readonly ILogger<DomainResolver> logger;

        private readonly TimeSpan lookupTimeout = TimeSpan.FromSeconds(1d);
        private readonly TimeSpan connectTimeout = TimeSpan.FromSeconds(2d);
        private readonly TimeSpan resolveCacheTimeSpan = TimeSpan.FromMinutes(2d);
        private readonly ConcurrentDictionary<DnsEndPoint, SemaphoreSlim> semaphoreSlims = new();

        /// <summary>
        /// 域名解析器
        /// </summary>
        /// <param name="memoryCache"></param>
        /// <param name="fastGithubConfig"></param>
        /// <param name="logger"></param>
        public DomainResolver(
            IMemoryCache memoryCache,
            FastGithubConfig fastGithubConfig,
            ILogger<DomainResolver> logger)
        {
            this.memoryCache = memoryCache;
            this.fastGithubConfig = fastGithubConfig;
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
                if (this.memoryCache.TryGetValue<IPAddress>(endPoint, out var address) == false)
                {
                    address = await this.LookupAsync(endPoint, cancellationToken);
                    this.memoryCache.Set(endPoint, address, this.resolveCacheTimeSpan);
                }
                return address;
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        /// 查找ip
        /// </summary>
        /// <param name="endPoint"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<IPAddress> LookupAsync(DnsEndPoint endPoint, CancellationToken cancellationToken)
        {
            var pureDnsTask = this.LookupCoreAsync(this.fastGithubConfig.PureDns, endPoint, cancellationToken);
            var fastDnsTask = this.LookupCoreAsync(this.fastGithubConfig.FastDns, endPoint, cancellationToken);

            var addresses = await Task.WhenAll(pureDnsTask, fastDnsTask);
            var fastAddress = await this.GetFastIPAddressAsync(addresses.SelectMany(item => item), endPoint.Port, cancellationToken);

            if (fastAddress != null)
            {
                this.logger.LogInformation($"[{endPoint.Host}->{fastAddress}]");
                return fastAddress;
            }
            throw new FastGithubException($"解析不到{endPoint.Host}可用的ip");
        }

        /// <summary>
        /// 查找ip
        /// </summary>
        /// <param name="dns"></param>
        /// <param name="endPoint"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<IEnumerable<IPAddress>> LookupCoreAsync(IPEndPoint dns, DnsEndPoint endPoint, CancellationToken cancellationToken)
        {
            try
            {
                var dnsClient = new DnsClient(dns);
                using var timeoutTokenSource = new CancellationTokenSource(this.lookupTimeout);
                using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutTokenSource.Token);
                return await dnsClient.Lookup(endPoint.Host, RecordType.A, linkedTokenSource.Token);
            }
            catch
            {
                this.logger.LogWarning($"dns({dns})无法解析{endPoint.Host}");
                return Enumerable.Empty<IPAddress>();
            }
        }

        /// <summary>
        /// 获取最快的ip
        /// </summary>
        /// <param name="addresses"></param>
        /// <param name="port"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<IPAddress?> GetFastIPAddressAsync(IEnumerable<IPAddress> addresses, int port, CancellationToken cancellationToken)
        {
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
