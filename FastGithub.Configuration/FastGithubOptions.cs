using System.Collections.Generic;

namespace FastGithub.Configuration
{
    /// <summary>
    /// FastGithub的配置
    /// </summary>
    public class FastGithubOptions
    {
        /// <summary>
        /// 未污染的dns
        /// </summary>
        public DnsConfig PureDns { get; set; } = new DnsConfig { IPAddress = "127.0.0.1", Port = 5533 };

        /// <summary>
        /// 速度快的dns
        /// </summary>
        public DnsConfig FastDns { get; set; } = new DnsConfig { IPAddress = "114.114.114.114", Port = 53 };

        /// <summary>
        /// 代理的域名配置
        /// </summary>
        public Dictionary<string, DomainConfig> DomainConfigs { get; set; } = new();
    }
}
