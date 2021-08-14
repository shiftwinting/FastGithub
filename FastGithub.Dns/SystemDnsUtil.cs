using FastGithub.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace FastGithub.Dns
{
    /// <summary>
    /// 系统域名服务工具
    /// </summary>
    [SupportedOSPlatform("windows")]
    static class SystemDnsUtil
    {
        /// <summary>
        /// www.baidu.com的ip
        /// </summary>
        private static readonly IPAddress www_baidu_com = IPAddress.Parse("183.232.231.172");

        [DllImport("iphlpapi")]
        private static extern int GetBestInterface(uint dwDestAddr, ref uint pdwBestIfIndex);

        /// <summary>
        /// 刷新DNS缓存
        /// </summary>
        [DllImport("dnsapi.dll", EntryPoint = "DnsFlushResolverCache", SetLastError = true)]
        public static extern void DnsFlushResolverCache();


        /// <summary>
        /// 通过远程地址查找匹配的网络适接口
        /// </summary>
        /// <param name="remoteAddress"></param>
        /// <returns></returns>
        private static NetworkInterface GetBestNetworkInterface(IPAddress remoteAddress)
        {
            var dwBestIfIndex = 0u;
            var dwDestAddr = BitConverter.ToUInt32(remoteAddress.GetAddressBytes());
            var errorCode = GetBestInterface(dwDestAddr, ref dwBestIfIndex);
            if (errorCode != 0)
            {
                throw new NetworkInformationException(errorCode);
            }

            var @interface = NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(item => item.GetIPProperties().GetIPv4Properties().Index == dwBestIfIndex)
                .FirstOrDefault();

            return @interface ?? throw new FastGithubException("找不到网络适配器用来设置dns");
        }


        /// <summary>
        /// 设置主dns
        /// </summary>
        /// <param name="primitive"></param>
        /// <exception cref="NetworkInformationException"></exception>
        /// <exception cref="NotSupportedException"></exception> 
        public static void DnsSetPrimitive(IPAddress primitive)
        {
            var @interface = GetBestNetworkInterface(www_baidu_com);
            var dnsAddresses = @interface.GetIPProperties().DnsAddresses;
            if (primitive.Equals(dnsAddresses.FirstOrDefault()) == false)
            {
                var nameServers = dnsAddresses.Prepend(primitive);
                SetNameServers(@interface, nameServers);
            }
        }

        /// <summary>
        /// 移除主dns
        /// </summary>
        /// <param name="primitive"></param>
        /// <exception cref="NetworkInformationException"></exception>
        /// <exception cref="NotSupportedException"></exception> 
        public static void DnsRemovePrimitive(IPAddress primitive)
        {
            var @interface = GetBestNetworkInterface(www_baidu_com);
            var dnsAddresses = @interface.GetIPProperties().DnsAddresses;
            if (primitive.Equals(dnsAddresses.FirstOrDefault()))
            {
                var nameServers = dnsAddresses.Skip(1);
                SetNameServers(@interface, nameServers);
            }
        }

        /// <summary>
        /// 设置网口的dns
        /// </summary>
        /// <param name="interface"></param>
        /// <param name="nameServers"></param>
        private static void SetNameServers(NetworkInterface @interface, IEnumerable<IPAddress> nameServers)
        {
            Netsh($@"interface ipv4 delete dns ""{@interface.Name}"" all");
            foreach (var address in nameServers)
            {
                Netsh($@"interface ipv4 add dns ""{@interface.Name}"" {address} validate=no");
            }

            static void Netsh(string arguments)
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
}