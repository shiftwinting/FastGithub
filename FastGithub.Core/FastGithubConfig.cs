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
        private readonly Dictionary<DomainMatch, DomainConfig> domainConfigs;

        /// <summary>
        /// 获取信任dns
        /// </summary>
        public IPEndPoint TrustedDns { get; }

        /// <summary>
        /// 获取非信任dns
        /// </summary>
        public IPEndPoint UnTrustedDns { get; }

        /// <summary>
        /// FastGithub配置
        /// </summary>
        /// <param name="options"></param>
        public FastGithubConfig(FastGithubOptions options)
        {
            this.TrustedDns = options.TrustedDns.ToIPEndPoint();
            this.UnTrustedDns = options.UntrustedDns.ToIPEndPoint();
            this.domainConfigs = options.DomainConfigs.ToDictionary(kv => new DomainMatch(kv.Key), kv => kv.Value);
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
        /// <param name="domainConfig"></param>
        /// <returns></returns>
        public bool TryGetDomainConfig(string domain, [MaybeNullWhen(false)] out DomainConfig domainConfig)
        {
            var key = this.domainConfigs.Keys.FirstOrDefault(item => item.IsMatch(domain));
            if (key == null)
            {
                domainConfig = default;
                return false;
            }
            return this.domainConfigs.TryGetValue(key, out domainConfig);
        }
    }
}
