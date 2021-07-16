using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace FastGithub
{
    public class FastGithubOptions
    {
        private DomainMatch[]? domainMatches;

        public DnsIPEndPoint TrustedDns { get; set; } = new DnsIPEndPoint { Address = "127.0.0.1", Port = 5533 };

        public DnsIPEndPoint UntrustedDns { get; set; } = new DnsIPEndPoint { Address = "114.1114.114.114", Port = 53 };

        public HashSet<string> DomainMatches { get; set; } = new();

        public bool IsMatch(string domain)
        {
            if (this.domainMatches == null)
            {
                this.domainMatches = this.DomainMatches.Select(item => new DomainMatch(item)).ToArray();
            }
            return this.domainMatches.Any(item => item.IsMatch(domain));
        }

        private class DomainMatch
        {
            private readonly Regex regex;
            private readonly string value;

            public DomainMatch(string value)
            {
                this.value = value;
                var pattern = Regex.Escape(value).Replace(@"\*", ".*");
                this.regex = new Regex($"^{pattern}$");
            }

            public bool IsMatch(string domain)
            {
                return this.regex.IsMatch(domain);
            }

            public override string ToString()
            {
                return this.value;
            }
        }
    }
}
