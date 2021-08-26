using System;
using System.Collections.Generic;

namespace FastGithub.Configuration
{
    /// <summary>
    /// FastGithub的配置
    /// </summary>
    public class FastGithubOptions
    {
        /// <summary>
        /// 监听配置
        /// </summary>
        public ListenConfig Listen { get; set; } = new ListenConfig();

        /// <summary>
        /// 回退的dns
        /// </summary>
        public DnsConfig[] FallbackDns { get; set; } = Array.Empty<DnsConfig>();

        /// <summary>
        /// 代理的域名配置
        /// </summary>
        public Dictionary<string, DomainConfig> DomainConfigs { get; set; } = new();
    }
}
