using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace FastGithub.Dns
{
    /// <summary>
    /// 域名服务工具
    /// </summary>
    [SupportedOSPlatform("windows")]
    static class NameServiceUtil
    {
        /// <summary>
        /// www.baidu.com的ip
        /// </summary>
        private static readonly IPAddress www_baidu_com = IPAddress.Parse("183.232.231.172");

        [DllImport("iphlpapi")]
        private static extern int GetBestInterface(uint dwDestAddr, ref uint pdwBestIfIndex);

        /// <summary>
        /// 通过远程地址查找匹配的网络适接口
        /// </summary>
        /// <param name="remoteAddress"></param>
        /// <returns></returns>
        private static NetworkInterface? GetBestNetworkInterface(IPAddress remoteAddress)
        {
            var dwBestIfIndex = 0u;
            var dwDestAddr = BitConverter.ToUInt32(remoteAddress.GetAddressBytes());
            var errorCode = GetBestInterface(dwDestAddr, ref dwBestIfIndex);
            if (errorCode != 0)
            {
                throw new NetworkInformationException(errorCode);
            }

            return NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(item => item.GetIPProperties().GetIPv4Properties().Index == dwBestIfIndex)
                .FirstOrDefault();
        }

        /// <summary>
        /// 设置域名服务
        /// </summary>
        /// <param name="nameServers"></param>
        /// <exception cref="NetworkInformationException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        public static void SetNameServers(params IPAddress[] nameServers)
        {
            var networkIF = GetBestNetworkInterface(www_baidu_com);
            if (networkIF == null)
            {
                throw new NotSupportedException("找不到网络适配器用来设置dns");
            }

            Netsh($@"interface ipv4 delete dns ""{networkIF.Name}"" all");
            foreach (var address in nameServers)
            {
                Netsh($@"interface ipv4 add dns ""{networkIF.Name}"" {address} validate=no");
            }
        }

        /// <summary>
        /// 执行Netsh
        /// </summary>
        /// <param name="arguments"></param>
        private static void Netsh(string arguments)
        {
            var netsh = new ProcessStartInfo
            {
                FileName = "netsh.exe",
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            Process.Start(netsh)?.WaitForExit();
        }
    }
}
