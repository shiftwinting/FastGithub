using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.DomainResolve
{
    /// <summary>
    /// IP状态服务
    /// 状态缓存5分钟
    /// 连接超时5秒
    /// </summary>
    sealed class IPAddressStatusService
    {
        private readonly TimeSpan brokeExpiration = TimeSpan.FromMinutes(1d);
        private readonly TimeSpan normalExpiration = TimeSpan.FromMinutes(5d);
        private readonly TimeSpan connectTimeout = TimeSpan.FromSeconds(5d);
        private readonly IMemoryCache statusCache = new MemoryCache(Options.Create(new MemoryCacheOptions()));

        /// <summary>
        /// 并行获取可连接的IP
        /// </summary>
        /// <param name="addresses"></param>
        /// <param name="port"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IPAddress[]> GetAvailableAddressesAsync(IEnumerable<IPAddress> addresses, int port, CancellationToken cancellationToken)
        {
            if (addresses.Any() == false)
            {
                return Array.Empty<IPAddress>();
            }

            var statusTasks = addresses.Select(item => this.GetStatusAsync(item, port, cancellationToken));
            var statusArray = await Task.WhenAll(statusTasks);
            return statusArray
                .Where(item => item.Elapsed < TimeSpan.MaxValue)
                .OrderBy(item => item.Elapsed)
                .Select(item => item.Address)
                .ToArray();
        }


        /// <summary>
        /// 获取IP状态
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<IPAddressStatus> GetStatusAsync(IPAddress address, int port, CancellationToken cancellationToken)
        {
            var endPoint = new IPEndPoint(address, port);
            if (this.statusCache.TryGetValue<IPAddressStatus>(endPoint, out var status))
            {
                return status;
            }

            var stopWatch = Stopwatch.StartNew();
            try
            {
                using var timeoutTokenSource = new CancellationTokenSource(this.connectTimeout);
                using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutTokenSource.Token);
                using var socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                await socket.ConnectAsync(endPoint, linkedTokenSource.Token);

                status = new IPAddressStatus(endPoint.Address, stopWatch.Elapsed);
                return this.statusCache.Set(endPoint, status, this.normalExpiration);
            }
            catch (Exception)
            {
                cancellationToken.ThrowIfCancellationRequested();

                status = new IPAddressStatus(endPoint.Address, TimeSpan.MaxValue);
                var expiration = NetworkInterface.GetIsNetworkAvailable() ? this.normalExpiration : this.brokeExpiration;
                return this.statusCache.Set(endPoint, status, expiration);
            }
            finally
            {
                stopWatch.Stop();
            }
        }
    }
}
