using System.Collections.Generic;

namespace FastGithub
{
    /// <summary>
    /// FastGithub的配置
    /// </summary>
    public class FastGithubOptions
    {
        /// <summary>
        /// 受信任的dns服务
        /// </summary>
        public DnsConfig TrustedDns { get; set; } = new DnsConfig { IPAddress = "127.0.0.1", Port = 5533 };

        /// <summary>
        /// 不受信任的dns服务
        /// </summary>
        public DnsConfig UntrustedDns { get; set; } = new DnsConfig { IPAddress = "114.114.114.114", Port = 53 };

        /// <summary>
        /// 代理的域名配置
        /// </summary>
        public Dictionary<string, DomainConfig> DomainConfigs { get; set; } = new();
    }
}
