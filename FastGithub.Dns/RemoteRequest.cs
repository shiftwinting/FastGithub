using DNS.Protocol;
using System.Buffers.Binary;
using System.Net;
using System.Net.NetworkInformation;

namespace FastGithub.Dns
{
    /// <summary>
    /// 远程请求
    /// </summary>
    sealed class RemoteRequest : Request
    {
        /// <summary>
        /// 获取远程地址
        /// </summary>
        public IPAddress RemoteAddress { get; }

        /// <summary>
        /// 远程请求
        /// </summary>
        /// <param name="request"></param>
        /// <param name="remoteAddress"></param>
        public RemoteRequest(Request request, IPAddress remoteAddress)
            : base(request)
        {
            this.RemoteAddress = remoteAddress;
        }

        /// <summary>
        /// 获取对应的本机地址
        /// </summary> 
        /// <returns></returns>
        public IPAddress? GetLocalAddress()
        {
            foreach (var @interface in NetworkInterface.GetAllNetworkInterfaces())
            {
                var addresses = @interface.GetIPProperties().UnicastAddresses;
                foreach (var item in addresses)
                {
                    if (IsInSubNet(item.IPv4Mask, item.Address, this.RemoteAddress))
                    {
                        return item.Address;
                    }
                }
            }
            return default;
        }

        /// <summary>
        /// 是否在相同的子网里
        /// </summary>
        /// <param name="mask"></param>
        /// <param name="local"></param>
        /// <param name="remote"></param>
        /// <returns></returns>
        private static bool IsInSubNet(IPAddress mask, IPAddress local, IPAddress remote)
        {
            if (local.AddressFamily != remote.AddressFamily)
            {
                return false;
            }

            var maskValue = GetValue(mask);
            var localValue = GetValue(local);
            var remoteValue = GetValue(remote);
            return (maskValue & localValue) == (maskValue & remoteValue);

            static long GetValue(IPAddress address)
            {
                var bytes = address.GetAddressBytes();
                return bytes.Length == sizeof(int)
                    ? BinaryPrimitives.ReadInt32BigEndian(bytes)
                    : BinaryPrimitives.ReadInt64BigEndian(bytes);
            }
        }
    }
}
