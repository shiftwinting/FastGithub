using System;
using System.Net;

namespace FastGithub.Scanner
{
    /// <summary>
    /// 域名与ip关系
    /// </summary>
    class DomainAddress : IEquatable<DomainAddress>
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
        /// 域名与ip关系
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="address"></param>
        public DomainAddress(string domain, IPAddress address)
        {
            this.Domain = domain;
            this.Address = address;
        }

        public override bool Equals(object? obj)
        {
            return obj is DomainAddress other && this.Equals(other);
        }

        public bool Equals(DomainAddress? other)
        {
            return other != null && other.Address.Equals(this.Address) && other.Domain == this.Domain;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.Domain, this.Address);
        }

        public override string ToString()
        {
            return $"{this.Domain}=>{this.Address}";
        }
    }
}
