using FastGithub.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.DomainResolve
{
    /// <summary>
    /// 域名解析器
    /// </summary> 
    sealed class DomainResolver : IDomainResolver
    {
        private record EndPointItem(string Host, int Port);
        private static readonly string dnsEndpointFile = "dnsendpoints.json";
        private static readonly SemaphoreSlim dnsEndpointLocker = new(1, 1);
        private static readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly DnsClient dnsClient;
        private readonly FastGithubConfig fastGithubConfig;
        private readonly ILogger<DomainResolver> logger;
        private readonly ConcurrentDictionary<DnsEndPoint, IPAddressElapsedCollection> dnsEndPointAddressElapseds = new();

        /// <summary>
        /// 域名解析器
        /// </summary>
        /// <param name="dnsClient"></param>
        /// <param name="fastGithubConfig"></param>
        /// <param name="logger"></param>
        public DomainResolver(
            DnsClient dnsClient,
            FastGithubConfig fastGithubConfig,
            ILogger<DomainResolver> logger)
        {
            this.dnsClient = dnsClient;
            this.fastGithubConfig = fastGithubConfig;
            this.logger = logger;

            foreach (var endPoint in this.ReadDnsEndPoints())
            {
                this.dnsEndPointAddressElapseds.TryAdd(endPoint, IPAddressElapsedCollection.Empty);
            }
        }


        /// <summary>
        /// 读取保存的节点
        /// </summary>
        /// <returns></returns>
        private IList<DnsEndPoint> ReadDnsEndPoints()
        {
            if (File.Exists(dnsEndpointFile) == false)
            {
                return Array.Empty<DnsEndPoint>();
            }

            try
            {
                dnsEndpointLocker.Wait();

                var utf8Json = File.ReadAllBytes(dnsEndpointFile);
                var endPointItems = JsonSerializer.Deserialize<EndPointItem[]>(utf8Json, jsonOptions);
                if (endPointItems == null)
                {
                    return Array.Empty<DnsEndPoint>();
                }

                var dnsEndPoints = new List<DnsEndPoint>();
                foreach (var item in endPointItems)
                {
                    if (this.fastGithubConfig.IsMatch(item.Host))
                    {
                        dnsEndPoints.Add(new DnsEndPoint(item.Host, item.Port));
                    }
                }
                return dnsEndPoints;
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex.Message, "读取dns记录异常");
                return Array.Empty<DnsEndPoint>();
            }
            finally
            {
                dnsEndpointLocker.Release();
            }
        }

        /// <summary>
        /// 保存节点到文件
        /// </summary>
        /// <param name="dnsEndPoints"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task WriteDnsEndPointsAsync(IEnumerable<DnsEndPoint> dnsEndPoints, CancellationToken cancellationToken)
        {
            try
            {
                await dnsEndpointLocker.WaitAsync(CancellationToken.None);

                var endPointItems = dnsEndPoints.Select(item => new EndPointItem(item.Host, item.Port)).ToArray();
                var utf8Json = JsonSerializer.SerializeToUtf8Bytes(endPointItems, jsonOptions);
                await File.WriteAllBytesAsync(dnsEndpointFile, utf8Json, cancellationToken);
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex.Message, "保存dns记录异常");
            }
            finally
            {
                dnsEndpointLocker.Release();
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
            if (this.dnsEndPointAddressElapseds.TryGetValue(endPoint, out var addressElapseds) && addressElapseds.IsEmpty == false)
            {
                this.logger.LogInformation($"{endPoint.Host}->{addressElapseds}");
                foreach (var addressElapsed in addressElapseds)
                {
                    yield return addressElapsed.Adddress;
                }
            }
            else
            {
                if (this.dnsEndPointAddressElapseds.TryAdd(endPoint, IPAddressElapsedCollection.Empty))
                {
                    await this.WriteDnsEndPointsAsync(this.dnsEndPointAddressElapseds.Keys, cancellationToken);
                }

                await foreach (var adddress in this.dnsClient.ResolveAsync(endPoint, fastSort: true, cancellationToken))
                {
                    this.logger.LogInformation($"{endPoint.Host}->{adddress}");
                    yield return adddress;
                }
            }
        }

        /// <summary>
        /// 对所有节点进行测速
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task TestAllEndPointsAsync(CancellationToken cancellationToken)
        {
            foreach (var keyValue in this.dnsEndPointAddressElapseds)
            {
                if (keyValue.Value.IsEmpty || keyValue.Value.IsExpired)
                {
                    var dnsEndPoint = keyValue.Key;
                    var addresses = new List<IPAddress>();
                    await foreach (var adddress in this.dnsClient.ResolveAsync(dnsEndPoint, fastSort: false, cancellationToken))
                    {
                        addresses.Add(adddress);
                    }

                    var addressElapseds = IPAddressElapsedCollection.Empty;
                    if (addresses.Count == 1)
                    {
                        var addressElapsed = new IPAddressElapsed(addresses[0], TimeSpan.Zero);
                        addressElapseds = new IPAddressElapsedCollection(addressElapsed);
                    }
                    else if (addresses.Count > 1)
                    {
                        var tasks = addresses.Select(address => IPAddressElapsed.ParseAsync(address, dnsEndPoint.Port, cancellationToken));
                        addressElapseds = new IPAddressElapsedCollection(await Task.WhenAll(tasks));
                    }
                    this.dnsEndPointAddressElapseds[dnsEndPoint] = addressElapseds;
                }
            }
        }
    }
}
