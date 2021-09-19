using FastGithub.Configuration;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace FastGithub.HttpServer
{
    /// <summary>
    /// https反向代理端口
    /// </summary>
    static class HttpsReverseProxyPort
    {
        /// <summary>
        /// 获取端口值
        /// </summary>
        public static int Value { get; } = OperatingSystem.IsWindows() ? 443 : GetAvailableTcpPort(AddressFamily.InterNetwork);

        /// <summary>
        /// 获取可用的随机Tcp端口
        /// </summary>
        /// <param name="addressFamily"></param>
        /// <param name="min">最小值</param>
        /// <returns></returns>
        private static int GetAvailableTcpPort(AddressFamily addressFamily, int min = 12345)
        {
            var hashSet = new HashSet<int>();
            var tcpListeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();

            foreach (var endpoint in tcpListeners)
            {
                if (endpoint.AddressFamily == addressFamily)
                {
                    hashSet.Add(endpoint.Port);
                }
            }

            for (var port = min; port < IPEndPoint.MaxPort; port++)
            {
                if (hashSet.Contains(port) == false)
                {
                    return port;
                }
            }

            throw new FastGithubException("当前无可用的端口");
        }
    }
}
