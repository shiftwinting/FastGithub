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
        /// 获取可用的随机Tcp端口
        /// </summary>
        /// <param name="addressFamily"></param>
        /// <param name="min">最小值</param>
        /// <returns></returns>
        public static int GetAvailableTcpPort(AddressFamily addressFamily, int min = 1025)
        {
            var hashSet = new HashSet<int>();
            var tcpListeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();

            foreach (var item in tcpListeners)
            {
                if (item.AddressFamily == addressFamily)
                {
                    hashSet.Add(item.Port);
                }
            }

            for (var port = min; port < ushort.MaxValue; port++)
            {
                if (hashSet.Contains(port) == false)
                {
                    return port;
                }
            }

            throw new FastGithubException("当前无可用的端口");
        }


        /// <summary>
        /// 获取可用的随机端口
        /// </summary>
        /// <param name="addressFamily"></param>
        /// <param name="min">最小值</param>
        /// <returns></returns>
        public static int GetAvailablePort(AddressFamily addressFamily, int min = 1025)
        {
            var hashSet = new HashSet<int>();
            var tcpListeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
            var udpListeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners();

            foreach (var item in tcpListeners)
            {
                if (item.AddressFamily == addressFamily)
                {
                    hashSet.Add(item.Port);
                }
            }

            foreach (var item in udpListeners)
            {
                if (item.AddressFamily == addressFamily)
                {
                    hashSet.Add(item.Port);
                }
            }

            for (var port = min; port < ushort.MaxValue; port++)
            {
                if (hashSet.Contains(port) == false)
                {
                    return port;
                }
            }

            throw new FastGithubException("当前无可用的端口");
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
    }
}
