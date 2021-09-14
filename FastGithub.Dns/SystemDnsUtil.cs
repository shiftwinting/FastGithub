using FastGithub.Configuration;
using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;

namespace FastGithub.Dns
{
    /// <summary>
    /// 系统域名服务工具
    /// </summary> 
    static class SystemDnsUtil
    {
        /// <summary>
        /// 设置为主dns
        /// </summary> 
        /// <exception cref="FastGithubException"></exception> 
        public static void SetAsPrimitiveDns()
        {
            var @interface = GetOutboundNetworkInterface();
            if (@interface == null)
            {
                throw new FastGithubException($"找不到匹配的网络适配器来设置主DNS");
            }

            var dnsAddresses = @interface.GetIPProperties().DnsAddresses;
            var firstRecord = dnsAddresses.FirstOrDefault();
            if (firstRecord == null || LocalMachine.ContainsIPAddress(firstRecord) == false)
            {
                var primitive = IPAddress.Loopback;
                if (OperatingSystem.IsLinux())
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
        /// 从主dns移除
        /// </summary> 
        /// <exception cref="FastGithubException"></exception> 
        public static void RemoveFromPrimitiveDns()
        {
            var @interface = GetOutboundNetworkInterface();
            if (@interface == null)
            {
                throw new FastGithubException($"找不到匹配的网络适配器来移除主DNS");
            }

            var dnsAddresses = @interface.GetIPProperties().DnsAddresses;
            var firstRecord = dnsAddresses.FirstOrDefault();
            if (firstRecord != null && LocalMachine.ContainsIPAddress(firstRecord))
            {
                if (OperatingSystem.IsLinux())
                {
                    throw new FastGithubException($"不支持自动移除本机主DNS，请手工移除/etc/resolv.conf的第一条记录");
                }
                else if (OperatingSystem.IsMacOS())
                {
                    throw new FastGithubException($"不支持自动移除本机主DNS，请手工移除连接网络的DNS的第一条记录");
                }
            }
        }


        /// <summary>
        /// 查找出口的网络适器
        /// </summary> 
        /// <returns></returns> 
        private static NetworkInterface? GetOutboundNetworkInterface()
        {
            var remoteEndPoint = new IPEndPoint(IPAddress.Parse("1.1.1.1"), 53);
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
    }
}