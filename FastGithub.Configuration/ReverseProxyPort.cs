using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;

namespace FastGithub.Configuration
{
    /// <summary>
    /// 反向代理端口
    /// </summary>
    public static class ReverseProxyPort
    {
        /// <summary>
        /// ssh端口
        /// </summary>
        public static int Ssh { get; } = OperatingSystem.IsWindows() ? GetAvailableTcpPort(22) : GetAvailableTcpPort(3822);

        /// <summary>
        /// http端口
        /// </summary>
        public static int Http { get; } = OperatingSystem.IsWindows() ? GetAvailableTcpPort(80) : GetAvailableTcpPort(3880);

        /// <summary>
        /// https端口
        /// </summary>
        public static int Https { get; } = OperatingSystem.IsWindows() ? GetAvailableTcpPort(443) : GetAvailableTcpPort(38443);

        /// <summary>
        /// 获取可用的随机Tcp端口
        /// </summary>
        /// <param name="minValue"></param> 
        /// <returns></returns>
        private static int GetAvailableTcpPort(int minValue)
        {
            var hashSet = new HashSet<int>();
            var tcpListeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();

            foreach (var endpoint in tcpListeners)
            {
                hashSet.Add(endpoint.Port);
            }

            for (var port = minValue; port < IPEndPoint.MaxPort; port++)
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
