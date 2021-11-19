using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.DomainResolve
{
    /// <summary>
    /// IP状态服务
    /// 连接成功的IP缓存5分钟
    /// 连接失败的IP缓存2分钟
    /// </summary>
    sealed class IPAddressStatusService
    {
        private readonly TimeSpan activeTTL = TimeSpan.FromMinutes(5d);
        private readonly TimeSpan negativeTTL = TimeSpan.FromMinutes(2d);
        private readonly TimeSpan connectTimeout = TimeSpan.FromSeconds(5d);
        private readonly IMemoryCache statusCache = new MemoryCache(Options.Create(new MemoryCacheOptions()));


        /// <summary>
        /// 并行获取多个IP的状态
        /// </summary>
        /// <param name="addresses"></param>
        /// <param name="port"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<IPAddressStatus[]> GetParallelAsync(IEnumerable<IPAddress> addresses, int port, CancellationToken cancellationToken)
        {
            var statusTasks = addresses.Select(item => this.GetAsync(item, port, cancellationToken));
            return Task.WhenAll(statusTasks);
        }

        /// <summary>
        /// 获取IP状态
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IPAddressStatus> GetAsync(IPAddress address, int port, CancellationToken cancellationToken)
        {
            var endPoint = new IPEndPoint(address, port);
            if (this.statusCache.TryGetValue<IPAddressStatus>(endPoint, out var status))
            {
                return status;
            }

            status = await this.GetAddressStatusAsync(endPoint, cancellationToken);
            var ttl = status.Elapsed < TimeSpan.MaxValue ? this.activeTTL : this.negativeTTL;
            return this.statusCache.Set(endPoint, status, ttl);
        }

        /// <summary>
        /// 获取IP状态
        /// </summary>
        /// <param name="endPoint"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<IPAddressStatus> GetAddressStatusAsync(IPEndPoint endPoint, CancellationToken cancellationToken)
        {
            var stopWatch = Stopwatch.StartNew();
            try
            {
                using var timeoutTokenSource = new CancellationTokenSource(this.connectTimeout);
                using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutTokenSource.Token);
                using var socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                await socket.ConnectAsync(endPoint, linkedTokenSource.Token);
                return new IPAddressStatus(endPoint.Address, stopWatch.Elapsed);
            }
            catch (Exception)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return new IPAddressStatus(endPoint.Address, TimeSpan.MaxValue);
            }
            finally
            {
                stopWatch.Stop();
            }
        }
    }
}
