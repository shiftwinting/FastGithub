using FastGithub.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Net.Sockets;

namespace FastGithub.ReverseProxy
{
    /// <summary>
    /// 端口管理服务
    /// </summary>
    public class PortService
    {
        private int httpsReverseProxyPort = -1;

        /// <summary>
        /// http代理端口
        /// </summary>
        public int HttpProxyPort { get; } 

        /// <summary>
        /// 获取https反向代理端口
        /// </summary>
        public int HttpsReverseProxyPort
        {
            get
            {
                if (OperatingSystem.IsWindows())
                {
                    return 443;
                }
                if (this.httpsReverseProxyPort < 0)
                {
                    this.httpsReverseProxyPort = LocalMachine.GetAvailablePort(AddressFamily.InterNetwork);
                }
                return this.httpsReverseProxyPort;
            }
        }

        /// <summary>
        /// 端口管理服务
        /// </summary>
        /// <param name="options"></param>
        public PortService(IOptions<FastGithubOptions> options)
        {
            this.HttpProxyPort = options.Value.HttpProxyPort;
        }
    }
}
