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



        /// <summary>
        /// 初始化选项为配置
        /// </summary>
        /// <exception cref="FastGithubException"></exception>
        public void InitConfig()
        {
            this.fastGithubConfig = new FastGithubConfig(this);
        }

        /// <summary>
        /// 配置
        /// </summary>
        private FastGithubConfig? fastGithubConfig;

        /// <summary>
        /// 获取配置
        /// </summary>
        public FastGithubConfig Config => this.fastGithubConfig!;
    }
}
