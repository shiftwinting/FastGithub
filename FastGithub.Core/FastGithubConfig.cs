using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;

namespace FastGithub
{
    /// <summary>
    /// FastGithub配置
    /// </summary>
    public class FastGithubConfig
    {
        /// <summary>
        /// 域名与配置缓存
        /// </summary>
        [AllowNull]
        private ConcurrentDictionary<string, DomainConfig?> domainConfigCache;


        /// <summary>
        /// 未污染的dns
        /// </summary>
        [AllowNull]
        public IPEndPoint PureDns { get; private set; }

        /// <summary>
        /// 速度快的dns
        /// </summary>
        [AllowNull]
        public IPEndPoint FastDns { get; private set; }

        /// <summary>
        /// 获取域名配置
        /// </summary>
        [AllowNull]
        public Dictionary<DomainMatch, DomainConfig> DomainConfigs { get; private set; }

        /// <summary>
        /// FastGithub配置
        /// </summary> 
        /// <param name="options"></param>
        public FastGithubConfig(IOptionsMonitor<FastGithubOptions> options)
        {
            this.Init(options.CurrentValue);
            options.OnChange(opt => this.Init(opt));
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="options"></param>
        private void Init(FastGithubOptions options)
        {
            this.domainConfigCache = new ConcurrentDictionary<string, DomainConfig?>();
            this.PureDns = options.PureDns.ToIPEndPoint();
            this.FastDns = options.FastDns.ToIPEndPoint();
            this.DomainConfigs = options.DomainConfigs.ToDictionary(kv => new DomainMatch(kv.Key), kv => kv.Value);
        }

        /// <summary>
        /// 是否匹配指定的域名
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        public bool IsMatch(string domain)
        {
            return this.DomainConfigs.Keys.Any(item => item.IsMatch(domain));
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
                var key = this.DomainConfigs.Keys.FirstOrDefault(item => item.IsMatch(domain));
                return key == null ? null : this.DomainConfigs[key];
            }
        }
    }
}
