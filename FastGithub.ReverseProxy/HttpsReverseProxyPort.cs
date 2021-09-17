using FastGithub.Configuration;
using System;
using System.Net.Sockets;

namespace FastGithub.ReverseProxy
{
    /// <summary>
    /// https反向代理端口
    /// </summary>
    static class HttpsReverseProxyPort
    {
        /// <summary>
        /// 获取端口值
        /// </summary>
        public static int Value { get; } = OperatingSystem.IsWindows() ? 443 : LocalMachine.GetAvailableTcpPort(AddressFamily.InterNetwork);
    }
}
