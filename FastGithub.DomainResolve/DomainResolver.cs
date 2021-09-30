using FastGithub.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
        private readonly DnsClient dnsClient;
        private readonly ConcurrentDictionary<DnsEndPoint, IPAddressTestResult> dnsEndPointAddressTestResult = new();

        /// <summary>
        /// 域名解析器
        /// </summary> 
        /// <param name="dnsClient"></param>
        public DomainResolver(DnsClient dnsClient)
        {
            this.dnsClient = dnsClient;
        }

        /// <summary>
        /// 预加载
        /// </summary>
        /// <param name="domain">域名</param>
        public void Prefetch(string domain)
        {
            var endPoint = new DnsEndPoint(domain, 443);
            this.dnsEndPointAddressTestResult.TryAdd(endPoint, IPAddressTestResult.Empty);
        }

        /// <summary>
        /// 对所有节点进行测速
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task TestAllEndPointsAsync(CancellationToken cancellationToken)
        {
            foreach (var keyValue in this.dnsEndPointAddressTestResult)
            {
                if (keyValue.Value.IsEmpty || keyValue.Value.IsExpired)
                {
                    var dnsEndPoint = keyValue.Key;
                    var addresses = new List<IPAddress>();
                    await foreach (var adddress in this.dnsClient.ResolveAsync(dnsEndPoint.Host, cancellationToken))
                    {
                        addresses.Add(adddress);
                    }

                    var addressTestResult = IPAddressTestResult.Empty;
                    if (addresses.Count == 1)
                    {
                        var addressElapseds = new[] { new IPAddressElapsed(addresses[0], TimeSpan.Zero) };
                        addressTestResult = new IPAddressTestResult(addressElapseds);
                    }
                    else if (addresses.Count > 1)
                    {
                        var tasks = addresses.Select(item => GetIPAddressElapsedAsync(item, dnsEndPoint.Port, cancellationToken));
                        var addressElapseds = await Task.WhenAll(tasks);
                        addressTestResult = new IPAddressTestResult(addressElapseds);
                    }
                    this.dnsEndPointAddressTestResult[dnsEndPoint] = addressTestResult;
                }
            }
        }

        /// <summary>
        /// 获取连接耗时
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private static async Task<IPAddressElapsed> GetIPAddressElapsedAsync(IPAddress address, int port, CancellationToken cancellationToken)
        {
            var stopWatch = Stopwatch.StartNew();
            try
            {
                using var timeoutTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10d));
                using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutTokenSource.Token);
                using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                await socket.ConnectAsync(address, port, linkedTokenSource.Token);
                return new IPAddressElapsed(address, stopWatch.Elapsed);
            }
            catch (Exception)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return new IPAddressElapsed(address, TimeSpan.MaxValue);
            }
            finally
            {
                stopWatch.Stop();
            }
        }

        /// <summary>
        /// 解析ip
        /// </summary>
        /// <param name="endPoint">节点</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IPAddress> ResolveAnyAsync(DnsEndPoint endPoint, CancellationToken cancellationToken = default)
        {
            await foreach (var address in this.ResolveAllAsync(endPoint, cancellationToken))
            {
                return address;
            }
            throw new FastGithubException($"解析不到{endPoint.Host}的IP");
        }

        /// <summary>
        /// 解析域名
        /// </summary>
        /// <param name="endPoint">节点</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async IAsyncEnumerable<IPAddress> ResolveAllAsync(DnsEndPoint endPoint, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (this.dnsEndPointAddressTestResult.TryGetValue(endPoint, out var speedTestResult) && speedTestResult.IsEmpty == false)
            {
                foreach (var addressElapsed in speedTestResult.AddressElapseds)
                {
                    yield return addressElapsed.Adddress;
                }
            }
            else
            {
                this.dnsEndPointAddressTestResult.TryAdd(endPoint, IPAddressTestResult.Empty);
                await foreach (var adddress in this.dnsClient.ResolveAsync(endPoint.Host, cancellationToken))
                {
                    yield return adddress;
                }
            }
        }
    }
}
