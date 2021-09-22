using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

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
        public static int Ssh { get; } = GetAvailableTcpPort(22);

        /// <summary>
        /// http端口
        /// </summary>
        public static int Http { get; } = GetAvailableTcpPort(80);

        /// <summary>
        /// https端口
        /// </summary>
        public static int Https { get; } = GetAvailableTcpPort(443);

        /// <summary>
        /// 获取可用的随机Tcp端口
        /// </summary>
        /// <param name="minValue"></param>
        /// <param name="addressFamily"></param>
        /// <returns></returns>
        private static int GetAvailableTcpPort(int minValue, AddressFamily addressFamily = AddressFamily.InterNetwork)
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
