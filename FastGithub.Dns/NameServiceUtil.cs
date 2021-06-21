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
            return errorCode != 0
                ? throw new NetworkInformationException(errorCode)
                : NetworkInterface
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
        /// <returns>未设置之前的记录</returns>
        public static IPAddress[] SetNameServers(params IPAddress[] nameServers)
        {
            var networkInterface = GetBestNetworkInterface(www_baidu_com);
            if (networkInterface == null)
            {
                throw new NotSupportedException("找不到网络适配器用来设置dns");
            }
            var dnsAddresses = networkInterface.GetIPProperties().DnsAddresses.ToArray();

            Netsh($@"interface ipv4 delete dns ""{networkInterface.Name}"" all");
            foreach (var address in nameServers)
            {
                Netsh($@"interface ipv4 add dns ""{networkInterface.Name}"" {address} validate=no");
            }

            return dnsAddresses;
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
