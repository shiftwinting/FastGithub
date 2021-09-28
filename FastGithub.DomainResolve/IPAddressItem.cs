using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace FastGithub.DomainResolve
{
    sealed class IPAddressItem : IEquatable<IPAddressItem>
    {
        public IPAddress Address { get; }

        public TimeSpan Elapsed { get; private set; } = TimeSpan.MaxValue;

        public IPAddressItem(IPAddress address)
        {
            this.Address = address;
        }

        public async Task TestSpeedAsync()
        {
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(this.Address);
                this.Elapsed = reply.Status == IPStatus.Success
                    ? TimeSpan.FromMilliseconds(reply.RoundtripTime)
                    : TimeSpan.MaxValue;
            }
            catch (Exception)
            {
                this.Elapsed = TimeSpan.MaxValue;
            }
        }

        public bool Equals(IPAddressItem? other)
        {
            return other != null && other.Address.Equals(this.Address);
        }

        public override bool Equals(object? obj)
        {
            return obj is IPAddressItem other && this.Equals(other);
        }

        public override int GetHashCode()
        {
            return this.Address.GetHashCode();
        }
    }
}
