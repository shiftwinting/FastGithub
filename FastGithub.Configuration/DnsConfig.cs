using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.NetworkInformation;

namespace FastGithub.Configuration
{
    /// <summary>
    /// dns配置
    /// </summary>
    public class DnsConfig
    {
        /// <summary>
        /// IP地址
        /// </summary>
        [AllowNull]
        public string IPAddress { get; set; }

        /// <summary>
        /// 端口
        /// </summary>
        public int Port { get; set; } = 53;

        /// <summary>
        /// 转换为IPEndPoint
        /// </summary>
        /// <returns></returns>
        /// <exception cref="FastGithubException"></exception>
        public IPEndPoint ToIPEndPoint()
        {
            if (System.Net.IPAddress.TryParse(this.IPAddress, out var address) == false)
            {
                throw new FastGithubException($"无效的ip：{this.IPAddress}");
            }

            if (this.Port == 53 && IsLocalMachineIPAddress(address))
            {
                throw new FastGithubException($"配置的dns值不能指向{nameof(FastGithub)}自身：{this.IPAddress}:{this.Port}");
            }

            return new IPEndPoint(address, this.Port);
        }

        public override string ToString()
        {
            return $"{this.IPAddress}:{this.Port}";
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
