using FastGithub.Configuration;
using Microsoft.Extensions.Logging;
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
    sealed class DomainSpeedTester : IDisposable
    {
        private const string DOMAINS_JSON_FILE = "domains.json";

        private readonly DnsClient dnsClient;
        private readonly ILogger<DomainSpeedTester> logger;

        private readonly object syncRoot = new();
        private readonly Dictionary<string, IPAddressItemHashSet> domainIPAddressHashSet = new();

        /// <summary>
        /// 域名的IP测速服务
        /// </summary>
        /// <param name="dnsClient"></param>
        /// <param name="logger"></param>
        public DomainSpeedTester(
            DnsClient dnsClient,
            ILogger<DomainSpeedTester> logger)
        {
            this.dnsClient = dnsClient;
            this.logger = logger;

            try
            {
                this.LoadDomains();
            }
            catch (Exception ex)
            {
                logger.LogWarning($"加载域名数据失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 加载域名数据
        /// </summary> 
        private void LoadDomains()
        {
            if (File.Exists(DOMAINS_JSON_FILE) == false)
            {
                return;
            }

            var utf8Json = File.ReadAllBytes(DOMAINS_JSON_FILE);
            var domains = JsonSerializer.Deserialize<string[]>(utf8Json);
            if (domains == null)
            {
                return;
            }

            foreach (var domain in domains)
            {
                this.domainIPAddressHashSet.TryAdd(domain, new IPAddressItemHashSet());
            }
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

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            try
            {
                this.SaveDomains();
            }
            catch (Exception ex)
            {
                this.logger.LogWarning($"保存域名数据失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 保存域名
        /// </summary>
        private void SaveDomains()
        {
            var domains = this.domainIPAddressHashSet.Keys
               .Select(item => new DomainPattern(item))
               .OrderBy(item => item)
               .Select(item => item.ToString())
               .ToArray();

            var utf8Json = JsonSerializer.SerializeToUtf8Bytes(domains, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllBytes(DOMAINS_JSON_FILE, utf8Json);
        }
    }
}
