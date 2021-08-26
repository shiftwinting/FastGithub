using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;

namespace FastGithub.Configuration
{
    /// <summary>
    /// FastGithub配置
    /// </summary>
    public class FastGithubConfig
    {
        private readonly ILogger<FastGithubConfig> logger;
        private SortedDictionary<DomainPattern, DomainConfig> domainConfigs;
        private ConcurrentDictionary<string, DomainConfig?> domainConfigCache;

        /// <summary>
        /// 回退的dns
        /// </summary>
        public IPEndPoint[] FallbackDns { get; set; }

        /// <summary>
        /// FastGithub配置
        /// </summary>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public FastGithubConfig(
            IOptionsMonitor<FastGithubOptions> options,
            ILogger<FastGithubConfig> logger)
        {
            this.logger = logger;
            var opt = options.CurrentValue;

            this.FallbackDns = opt.FallbackDns.Select(item => item.ToIPEndPoint()).ToArray();
            this.domainConfigs = ConvertDomainConfigs(opt.DomainConfigs);
            this.domainConfigCache = new ConcurrentDictionary<string, DomainConfig?>();

            options.OnChange(opt => this.Update(opt));
        }

        /// <summary>
        /// 更新配置
        /// </summary>
        /// <param name="options"></param>
        private void Update(FastGithubOptions options)
        {
            try
            {
                this.FallbackDns = options.FallbackDns.Select(item => item.ToIPEndPoint()).ToArray();
                this.domainConfigs = ConvertDomainConfigs(options.DomainConfigs);
                this.domainConfigCache = new ConcurrentDictionary<string, DomainConfig?>();
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex.Message);
            }
        }

        /// <summary>
        /// 配置转换
        /// </summary>
        /// <param name="domainConfigs"></param>
        /// <returns></returns>
        private static SortedDictionary<DomainPattern, DomainConfig> ConvertDomainConfigs(Dictionary<string, DomainConfig> domainConfigs)
        {
            var result = new SortedDictionary<DomainPattern, DomainConfig>();
            foreach (var kv in domainConfigs)
            {
                result.Add(new DomainPattern(kv.Key), kv.Value);
            }
            return result;
        }

        /// <summary>
        /// 是否匹配指定的域名
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        public bool IsMatch(string domain)
        {
            return this.domainConfigs.Keys.Any(item => item.IsMatch(domain));
        }

        /// <summary>
        /// 尝试获取域名配置
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetDomainConfig(string domain, [MaybeNullWhen(false)] out DomainConfig value)
        {
            value = this.domainConfigCache.GetOrAdd(domain, GetDomainConfig);
            return value != null;

            DomainConfig? GetDomainConfig(string domain)
            {
                var key = this.domainConfigs.Keys.FirstOrDefault(item => item.IsMatch(domain));
                return key == null ? null : this.domainConfigs[key];
            }
        }
    }
}
