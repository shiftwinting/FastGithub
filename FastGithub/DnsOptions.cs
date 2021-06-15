using System.Net;

namespace FastGithub
{
    class DnsOptions
    {
        public IPAddress UpStream { get; set; } = IPAddress.Parse("114.114.114.114");
    }
}
