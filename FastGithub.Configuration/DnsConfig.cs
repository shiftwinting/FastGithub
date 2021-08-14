using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace FastGithub.Configuration
{
    /// <summary>
    /// dns配置
    /// </summary>
    public record DnsConfig
    {
        /// <summary>
        /// IP地址
        /// </summary>
        [AllowNull]
        public string IPAddress { get; init; }

        /// <summary>
        /// 端口
        /// </summary>
        public int Port { get; init; } = 53;

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

            if (this.Port == 53 && LocalMachine.ContainsIPAddress(address))
            {
                throw new FastGithubException($"配置的dns值不能指向{nameof(FastGithub)}自身：{this.IPAddress}:{this.Port}");
            }

            return new IPEndPoint(address, this.Port);
        }
    }
}
