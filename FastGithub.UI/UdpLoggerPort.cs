using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace FastGithub.UI
{
    /// <summary>
    /// udp日志工具类
    /// </summary>
    static class UdpLoggerPort
    {
        /// <summary>
        /// 获取日志端口
        /// </summary>
        public static int Value { get; } = GetAvailableUdpPort(38457);

        /// <summary>
        /// 获取可用的随机Udp端口
        /// </summary>
        /// <param name="minValue"></param>
        /// <param name="addressFamily"></param>
        /// <returns></returns>
        private static int GetAvailableUdpPort(int minValue, AddressFamily addressFamily = AddressFamily.InterNetwork)
        {
            var hashSet = new HashSet<int>();
            var tcpListeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners();

            foreach (var endpoint in tcpListeners)
            {
                if (endpoint.AddressFamily == addressFamily)
                {
                    hashSet.Add(endpoint.Port);
                }
            }

            for (var port = minValue; port < IPEndPoint.MaxPort; port++)
            {
                if (hashSet.Contains(port) == false)
                {
                    return port;
                }
            }

            throw new ArgumentException("当前无可用的端口");
        }
    }
}
