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
        /// 获取本机设备所有IP
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
        /// 获取本机设备所有IPv4
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
        /// 返回本机设备是否包含指定IP
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static bool ContainsIPAddress(IPAddress address)
        {
            return GetAllIPAddresses().Contains(address);
        }

        /// <summary>
        /// 获取与远程节点通讯的的本机IP地址
        /// </summary>
        /// <param name="remoteEndPoint">远程地址</param>
        /// <returns></returns>
        public static IPAddress? GetLocalIPAddress(EndPoint remoteEndPoint)
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

        /// <summary>
        /// 是否可以监听指定tcp端口
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public static bool CanListenTcp(int port)
        {
            var tcpListeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
            return tcpListeners.Any(item => item.Port == port) == false;
        }

        /// <summary>
        /// 是否可以监听指定udp端口
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public static bool CanListenUdp(int port)
        {
            var udpListeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners();
            return udpListeners.Any(item => item.Port == port) == false;
        }
    }
}
