using System;
using System.Net;

namespace FastGithub
{
    class GithubContext : IEquatable<GithubContext>
    {
        public string Domain { get; }

        public IPAddress Address { get; }

        public TimeSpan? HttpElapsed { get; set; }

        public GithubContext(string domain, IPAddress address)
        {
            this.Domain = domain;
            this.Address = address;
        }

        public override string ToString()
        {
            return $"{Address}\t{Domain}\t# {HttpElapsed}";
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
