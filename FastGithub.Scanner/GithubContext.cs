using System;
using System.Net;

namespace FastGithub.Scanner
{
    sealed class GithubContext : DomainAddress, IEquatable<GithubContext>
    {
        private record Github(
            string Domain,
            IPAddress Address,
            double AvailableRate,
            double AvgElapsed);

        /// <summary>
        /// 获取或设置是否可用
        /// </summary>
        public bool Available { get; set; }

        /// <summary>
        /// 获取扫描历史信息
        /// </summary>
        public GithubContextHistory History { get; } = new();


        public GithubContext(string domain, IPAddress address)
            : base(domain, address)
        {
        }

        public bool Equals(GithubContext? other)
        {
            return base.Equals(other);
        }

        public override string ToString()
        {
            return new Github(
                this.Domain,
                this.Address,
                this.History.AvailableRate,
                this.History.AvgElapsed.TotalSeconds
                ).ToString();
        }
    }
}
