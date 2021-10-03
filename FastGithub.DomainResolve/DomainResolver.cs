using FastGithub.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
        private readonly ConcurrentDictionary<DnsEndPoint, IPAddressElapsedCollection> dnsEndPointAddressElapseds = new();

        /// <summary>
        /// 域名解析器
        /// </summary> 
        /// <param name="dnsClient"></param>
        public DomainResolver(DnsClient dnsClient)
        {
            this.dnsClient = dnsClient;
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
                foreach (var addressElapsed in addressElapseds)
                {
                    yield return addressElapsed.Adddress;
                }
            }
            else
            {
                this.dnsEndPointAddressElapseds.TryAdd(endPoint, IPAddressElapsedCollection.Empty);
                await foreach (var adddress in this.dnsClient.ResolveAsync(endPoint, cancellationToken))
                {
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
                    await foreach (var adddress in this.dnsClient.ResolveAsync(dnsEndPoint, cancellationToken))
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
