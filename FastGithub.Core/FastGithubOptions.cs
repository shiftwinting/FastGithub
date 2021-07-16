using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

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

        /// <summary>
        /// 受信任的dns服务
        /// </summary>
        public DnsIPEndPoint TrustedDns { get; set; } = new DnsIPEndPoint { IPAddress = "127.0.0.1", Port = 5533 };

        /// <summary>
        /// 不受信任的dns服务
        /// </summary>
        public DnsIPEndPoint UntrustedDns { get; set; } = new DnsIPEndPoint { IPAddress = "114.114.114.114", Port = 53 };

        /// <summary>
        /// 代理的域名匹配
        /// </summary>
        public HashSet<string> DomainMatches { get; set; } = new();

        /// <summary>
        /// 是否匹配指定的域名
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        public bool IsMatch(string domain)
        {
            if (this.domainMatches == null)
            {
                this.domainMatches = this.DomainMatches.Select(item => new DomainMatch(item)).ToArray();
            }
            return this.domainMatches.Any(item => item.IsMatch(domain));
        }

        /// <summary>
        /// 域名匹配
        /// </summary>
        private class DomainMatch
        {
            private readonly Regex regex;
            private readonly string pattern;

            /// <summary>
            /// 域名匹配
            /// </summary>
            /// <param name="pattern">域名表达式</param>
            public DomainMatch(string pattern)
            {
                this.pattern = pattern;
                var regexPattern = Regex.Escape(pattern).Replace(@"\*", ".*");
                this.regex = new Regex($"^{regexPattern}$", RegexOptions.IgnoreCase);
            }

            /// <summary>
            /// 是否与指定域名匹配
            /// </summary>
            /// <param name="domain"></param>
            /// <returns></returns>
            public bool IsMatch(string domain)
            {
                return this.regex.IsMatch(domain);
            }

            /// <summary>
            /// 转换为文本
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return this.pattern;
            }
        }
    }
}
