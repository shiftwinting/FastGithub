using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace FastGithub
{
    /// <summary>
    /// FastGithub的配置
    /// </summary>
    public class FastGithubOptions
    {
        /// <summary>
        /// 域名
        /// </summary>
        private DomainMatch[]? domainMatches;
        private IPEndPoint? trustedDnsIPEndPoint;
        private IPEndPoint? unTrustedDnsIPEndPoint;

        /// <summary>
        /// 受信任的dns服务
        /// </summary>
        public DnsConfig TrustedDns { get; set; } = new DnsConfig { IPAddress = "127.0.0.1", Port = 5533 };

        /// <summary>
        /// 不受信任的dns服务
        /// </summary>
        public DnsConfig UntrustedDns { get; set; } = new DnsConfig { IPAddress = "114.114.114.114", Port = 53 };

        /// <summary>
        /// 代理的域名表达式
        /// </summary>
        public HashSet<string> DomainPatterns { get; set; } = new();

        /// <summary>
        /// 验证选项值
        /// </summary>
        /// <exception cref="FastGithubException"></exception>
        public void Validate()
        {
            this.trustedDnsIPEndPoint = this.TrustedDns.ToIPEndPoint();
            this.unTrustedDnsIPEndPoint = this.UntrustedDns.ToIPEndPoint();
            this.domainMatches = this.DomainPatterns.Select(item => new DomainMatch(item)).ToArray();
        }


        /// <summary>
        /// 受信任的dns服务节点
        /// </summary>
        public IPEndPoint GetTrustedDns()
        {
            return this.trustedDnsIPEndPoint ?? throw new InvalidOperationException();
        }

        /// <summary>
        /// 不受信任的dns服务节点
        /// </summary>
        public IPEndPoint GetUnTrustedDns()
        {
            return this.unTrustedDnsIPEndPoint ?? throw new InvalidOperationException();
        }

        /// <summary>
        /// 是否匹配指定的域名
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        public bool IsMatch(string domain)
        {
            if (this.domainMatches == null)
            {
                throw new InvalidOperationException();
            }
            return this.domainMatches.Any(item => item.IsMatch(domain));
        }
    }
}
