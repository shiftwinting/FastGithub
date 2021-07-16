using System.Diagnostics.CodeAnalysis;
using System.Net;

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
        /// 验证
        /// </summary>
        /// <returns></returns>
        public bool Validate()
        {
            return System.Net.IPAddress.TryParse(this.IPAddress, out var address)
                ? !(address.Equals(System.Net.IPAddress.Loopback) && this.Port == 53)
                : false;
        }
    }
}
