using System;
using System.Net;

namespace FastGithub.Scanner
{
    sealed class GithubContext : IEquatable<GithubContext>
    {
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
        public bool Available { get; set; } = false;

        /// <summary>
        /// 获取或设置扫描总耗时
        /// </summary>
        public TimeSpan Elapsed { get; set; } = TimeSpan.MaxValue;


        public GithubContext(string domain, IPAddress address)
        {
            this.Domain = domain;
            this.Address = address;
        }

        public override string ToString()
        {
            return $"{Address}\t{Domain}\t# {Elapsed}";
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
    }
}
