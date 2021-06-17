using System;
using System.Net;

namespace FastGithub.Scanner
{
    sealed class GithubContext : IEquatable<GithubContext>
    {
        private record Github(
            string Domain,
            IPAddress Address,
            double SuccessRate,
            TimeSpan AvgElapsed);

        /// <summary>
        /// 获取域名
        /// </summary>
        public string Domain { get; }

        /// <summary>
        /// 获取ip
        /// </summary>
        public IPAddress Address { get; }

        /// <summary>
        /// 获取或设置是否可用
        /// </summary>
        public bool Available { get; set; }

        /// <summary>
        /// 获取扫描历史信息
        /// </summary>
        public GithubContextHistory History { get; } = new();


        public GithubContext(string domain, IPAddress address)
        {
            this.Domain = domain;
            this.Address = address;
        }

        public override bool Equals(object? obj)
        {
            return obj is GithubContext other && this.Equals(other);
        }

        public bool Equals(GithubContext? other)
        {
            return other != null && other.Address.Equals(this.Address) && other.Domain == this.Domain;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.Domain, this.Address);
        }

        public override string ToString()
        {
            return new Github(
                this.Domain,
                this.Address,
                this.History.GetSuccessRate(),
                this.History.GetAvgElapsed()
                ).ToString();
        }
    }
}
