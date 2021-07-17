using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.NetworkInformation;

namespace FastGithub
{
    /// <summary>
    /// dns的终节点
    /// </summary>
    public class DnsIPEndPoint
    {
        /// <summary>
        /// IP地址
        /// </summary>
        [AllowNull]
        public string IPAddress { get; set; }

        /// <summary>
        /// 端口
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// 转换为IPEndPoint
        /// </summary>
        /// <returns></returns>
        public IPEndPoint ToIPEndPoint()
        {
            return new IPEndPoint(System.Net.IPAddress.Parse(this.IPAddress), this.Port);
        }

        /// <summary>
        /// 验证dns
        /// 防止使用自己使用自己来解析域名造成死循环
        /// </summary>
        /// <returns></returns>
        public bool Validate()
        {
            if (System.Net.IPAddress.TryParse(this.IPAddress, out var address) == false)
            {
                return false;
            }

            if (this.Port == 53 && IsLocalMachineIPAddress(address))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 是否为本机ip
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        private static bool IsLocalMachineIPAddress(IPAddress address)
        {
            foreach (var @interface in NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (var addressInfo in @interface.GetIPProperties().UnicastAddresses)
                {
                    if (addressInfo.Address.Equals(address))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
