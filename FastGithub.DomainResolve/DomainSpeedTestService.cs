using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.DomainResolve
{
    /// <summary>
    /// 域名的IP测速服务
    /// </summary>
    sealed class DomainSpeedTestService
    {
        private const string DATA_FILE = "domains.json";
        private readonly DnsClient dnsClient;

        private readonly object syncRoot = new();
        private readonly Dictionary<string, IPAddressItemHashSet> domainIPAddressHashSet = new();

        /// <summary>
        /// 域名的IP测速服务
        /// </summary>
        /// <param name="dnsClient"></param>
        public DomainSpeedTestService(DnsClient dnsClient)
        {
            this.dnsClient = dnsClient;
        }

        /// <summary>
        /// 添加要测速的域名
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        public bool Add(string domain)
        {
            lock (this.syncRoot)
            {
                return this.domainIPAddressHashSet.TryAdd(domain, new IPAddressItemHashSet());
            }
        }

        /// <summary>
        /// 获取测试后排序的IP
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        public IPAddress[] GetIPAddresses(string domain)
        {
            lock (this.syncRoot)
            {
                if (this.domainIPAddressHashSet.TryGetValue(domain, out var hashSet) && hashSet.Count > 0)
                {
                    return hashSet.ToArray().OrderBy(item => item.PingElapsed).Select(item => item.Address).ToArray();
                }
                return Array.Empty<IPAddress>();
            }
        }

        /// <summary>
        /// 加载数据
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task LoadDataAsync(CancellationToken cancellationToken)
        {
            if (File.Exists(DATA_FILE) == false)
            {
                return;
            }

            var fileStream = File.OpenRead(DATA_FILE);
            var domains = await JsonSerializer.DeserializeAsync<string[]>(fileStream, cancellationToken: cancellationToken);
            if (domains == null)
            {
                return;
            }

            lock (this.syncRoot)
            {
                foreach (var domain in domains)
                {
                    this.domainIPAddressHashSet.TryAdd(domain, new IPAddressItemHashSet());
                }
            }
        }

        /// <summary>
        /// 保存数据
        /// </summary>
        /// <returns></returns>
        public async Task SaveDataAsync()
        {
            var domains = this.domainIPAddressHashSet.Keys.ToArray();
            using var fileStream = File.OpenWrite(DATA_FILE);
            await JsonSerializer.SerializeAsync(fileStream, domains);
        }

        /// <summary>
        /// 进行一轮IP测速
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task TestSpeedAsync(CancellationToken cancellationToken)
        {
            KeyValuePair<string, IPAddressItemHashSet>[] keyValues;
            lock (this.syncRoot)
            {
                keyValues = this.domainIPAddressHashSet.ToArray();
            }

            foreach (var keyValue in keyValues)
            {
                var domain = keyValue.Key;
                var hashSet = keyValue.Value;
                await foreach (var address in this.dnsClient.ResolveAsync(domain, cancellationToken))
                {
                    hashSet.Add(new IPAddressItem(address));
                }
                await hashSet.PingAllAsync();
            }
        }
    }
}
