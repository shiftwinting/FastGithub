using System;
using System.Net;

namespace FastGithub.Dns
{
    [Options("Dns")]
    sealed class DnsOptions
    {
        public IPAddress UpStream { get; set; } = IPAddress.Parse("114.114.114.114");

        public TimeSpan GithubTTL { get; set; } = TimeSpan.FromMinutes(10d);
    }
}
