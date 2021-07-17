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
        private IPEndPoint? trustedDnsEndPoint;
        private IPEndPoint? unTrustedDnsEndPoint;

        /// <summary>
        /// 受信任的dns服务
        /// </summary>
        public IPEndPointOptions TrustedDns { get; set; } = new IPEndPointOptions { IPAddress = "127.0.0.1", Port = 5533 };

        /// <summary>
        /// 不受信任的dns服务
        /// </summary>
        public IPEndPointOptions UntrustedDns { get; set; } = new IPEndPointOptions { IPAddress = "114.114.114.114", Port = 53 };

        /// <summary>
        /// 代理的域名匹配
        /// </summary>
        public HashSet<string> DomainMatches { get; set; } = new();

        /// <summary>
        /// 验证选项值
        /// </summary>
        /// <exception cref="FastGithubException"></exception>
        public void Validate()
        {
            this.trustedDnsEndPoint = this.TrustedDns.ToIPEndPoint();
            this.unTrustedDnsEndPoint = this.UntrustedDns.ToIPEndPoint();
            this.domainMatches = this.DomainMatches.Select(item => new DomainMatch(item)).ToArray();
        }


        /// <summary>
        /// 受信任的dns服务节点
        /// </summary>
        public IPEndPoint GetTrustedDns()
        {
            return this.trustedDnsEndPoint ?? throw new InvalidOperationException();
        }

        /// <summary>
        /// 不受信任的dns服务节点
        /// </summary>
        public IPEndPoint GetUnTrustedDns()
        {
            return this.unTrustedDnsEndPoint ?? throw new InvalidOperationException();
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
