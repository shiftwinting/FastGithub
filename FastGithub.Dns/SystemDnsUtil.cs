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
    static class SystemDnsUtil
    {
        /// <summary>
        /// www.baidu.com的ip
        /// </summary>
        private static readonly IPAddress www_baidu_com = IPAddress.Parse("183.232.231.172");

        /// <summary>
        /// 刷新DNS缓存
        /// </summary>
        [SupportedOSPlatform("windows")]
        [DllImport("dnsapi.dll", EntryPoint = "DnsFlushResolverCache", SetLastError = true)]
        private static extern void DnsFlushResolverCache();

        /// <summary>
        /// 刷新DNS缓存
        /// </summary>
        public static void FlushResolverCache()
        {
            if (OperatingSystem.IsWindows())
            {
                DnsFlushResolverCache();
            }
        }

        /// <summary>
        /// 设置主dns
        /// </summary>
        /// <param name="primitive"></param>
        /// <exception cref="FastGithubException"></exception> 
        public static void SetPrimitiveDns(IPAddress primitive)
        {
            var @interface = GetOutboundNetworkInterface();
            if (@interface == null)
            {
                throw new FastGithubException($"找不到匹配的网络适配器来设置主DNS值：{primitive}");
            }

            var dnsAddresses = @interface.GetIPProperties().DnsAddresses;
            if (primitive.Equals(dnsAddresses.FirstOrDefault()) == false)
            {
                var nameServers = dnsAddresses.Prepend(primitive);
                if (OperatingSystem.IsWindows())
                {
                    SetNameServers(@interface, nameServers);
                }
                else if (OperatingSystem.IsLinux())
                {
                    throw new FastGithubException($"不支持自动设置本机DNS，请手工添加{primitive}做为/etc/resolv.conf的第一条记录");
                }
                else if (OperatingSystem.IsMacOS())
                {
                    throw new FastGithubException($"不支持自动设置本机DNS，请手工添加{primitive}做为连接网络的DNS的第一条记录");
                }
            }
        }

        /// <summary>
        /// 移除主dns
        /// </summary>
        /// <param name="primitive"></param>
        /// <exception cref="FastGithubException"></exception> 
        public static void RemovePrimitiveDns(IPAddress primitive)
        {
            var @interface = GetOutboundNetworkInterface();
            if (@interface == null)
            {
                throw new FastGithubException($"找不到匹配的网络适配器来移除主DNS值：{primitive}");
            }

            var dnsAddresses = @interface.GetIPProperties().DnsAddresses;
            if (primitive.Equals(dnsAddresses.FirstOrDefault()))
            {
                var nameServers = dnsAddresses.Skip(1);
                if (OperatingSystem.IsWindows())
                {
                    SetNameServers(@interface, nameServers);
                }
            }
        }


        /// <summary>
        /// 查找出口的网络适器
        /// </summary> 
        /// <returns></returns> 
        private static NetworkInterface? GetOutboundNetworkInterface()
        {
            var remoteEndPoint = new IPEndPoint(www_baidu_com, 443);
            var address = LocalMachine.GetLocalIPAddress(remoteEndPoint);
            if (address == null)
            {
                return default;
            }

            return NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(item => item.GetIPProperties().UnicastAddresses.Any(a => a.Address.Equals(address)))
                .FirstOrDefault();
        }


        /// <summary>
        /// 设置网口的dns
        /// </summary>
        /// <param name="interface"></param>
        /// <param name="nameServers"></param>
        [SupportedOSPlatform("windows")]
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