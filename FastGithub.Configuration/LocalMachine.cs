using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace FastGithub.Configuration
{
    /// <summary>
    /// 提供本机设备信息
    /// </summary>
    public static class LocalMachine
    {
        /// <summary>
        /// 获取设备名
        /// </summary>
        public static string Name => Environment.MachineName;

        /// <summary>
        /// 获取设备所有IP
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<IPAddress> GetAllIPAddresses()
        {
            yield return IPAddress.Loopback;
            yield return IPAddress.IPv6Loopback;

            foreach (var @interface in NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (var addressInfo in @interface.GetIPProperties().UnicastAddresses)
                {
                    yield return addressInfo.Address;
                }
            }
        }

        /// <summary>
        /// 获取设备所有IPv4
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<IPAddress> GetAllIPv4Addresses()
        {
            foreach (var address in GetAllIPAddresses())
            {
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    yield return address;
                }
            }
        }

        /// <summary>
        /// 返回设备是否包含指定IP
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static bool ContainsIPAddress(IPAddress address)
        {
            return GetAllIPAddresses().Contains(address);
        }

        /// <summary>
        /// 获取对应的本机地址
        /// </summary>
        /// <param name="remoteEndPoint">远程地址</param>
        /// <returns></returns>
        public static IPAddress? GetLocalAddress(EndPoint remoteEndPoint)
        {
            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.Connect(remoteEndPoint);
                return socket.LocalEndPoint is IPEndPoint localEndPoint ? localEndPoint.Address : default;
            }
            catch (Exception)
            {
                return default;
            }
        }
    }
}
