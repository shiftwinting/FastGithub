using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace FastGithub
{
    public class DnsIPEndPoint
    {
        [AllowNull]
        public string Address { get; set; } = IPAddress.Loopback.ToString();

        public int Port { get; set; } = 53;

        public IPEndPoint ToIPEndPoint()
        {
            return new IPEndPoint(IPAddress.Parse(this.Address), this.Port);
        }

        public bool Validate()
        {
            return IPAddress.TryParse(this.Address, out var address) &&
                !(address.Equals(IPAddress.Loopback) && this.Port == 53);
        }
    }
}
