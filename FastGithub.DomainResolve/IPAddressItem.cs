using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace FastGithub.DomainResolve
{
    /// <summary>
    /// IP地址项
    /// </summary>
    [DebuggerDisplay("Address = {Address}, PingElapsed = {PingElapsed}")]
    sealed class IPAddressItem : IEquatable<IPAddressItem>
    {
        private readonly Ping ping = new();

        /// <summary>
        /// 地址
        /// </summary>
        public IPAddress Address { get; }

        /// <summary>
        /// Ping耗时
        /// </summary>
        public TimeSpan PingElapsed { get; private set; } = TimeSpan.MaxValue;

        /// <summary>
        /// IP地址项
        /// </summary>
        /// <param name="address"></param>
        public IPAddressItem(IPAddress address)
        {
            this.Address = address;
        }

        /// <summary>
        /// 发起ping请求
        /// </summary>
        /// <returns></returns>
        public async Task PingAsync()
        {
            try
            {
                var reply = await this.ping.SendPingAsync(this.Address);
                this.PingElapsed = reply.Status == IPStatus.Success
                    ? TimeSpan.FromMilliseconds(reply.RoundtripTime)
                    : TimeSpan.MaxValue;
            }
            catch (Exception)
            {
                this.PingElapsed = TimeSpan.MaxValue;
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
