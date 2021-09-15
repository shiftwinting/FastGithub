using FastGithub.Configuration;
using FastGithub.ReverseProxy;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net;

namespace FastGithub
{
    /// <summary>
    /// Kestrel扩展
    /// </summary>
    public static class KestrelServerOptionsExtensions
    {
        /// <summary>
        /// 无限制
        /// </summary>
        /// <param name="kestrel"></param>
        public static void NoLimit(this KestrelServerOptions kestrel)
        {
            kestrel.Limits.MaxRequestBodySize = null;
        }

        /// <summary>
        /// 监听http代理
        /// </summary>
        /// <param name="kestrel"></param>
        public static void ListenHttpProxy(this KestrelServerOptions kestrel)
        {
            var options = kestrel.ApplicationServices.GetRequiredService<IOptions<FastGithubOptions>>().Value;
            var httpProxyPort = options.HttpProxyPort;

            if (LocalMachine.CanListenTcp(httpProxyPort) == false)
            {
                throw new FastGithubException($"tcp端口{httpProxyPort}已经被其它进程占用，请在配置文件更换{nameof(FastGithubOptions.HttpProxyPort)}为其它端口");
            }

            var logger = kestrel.GetLogger();
            kestrel.Listen(IPAddress.Loopback, httpProxyPort);
            logger.LogInformation($"已监听http://127.0.0.1:{httpProxyPort}，http代理启动完成");
        }

        /// <summary>
        /// 尝试监听ssh反向代理
        /// </summary>
        /// <param name="kestrel"></param>
        public static void ListenSshReverseProxy(this KestrelServerOptions kestrel)
        {
            const int SSH_PORT = 22;
            if (LocalMachine.CanListenTcp(SSH_PORT) == true)
            {
                kestrel.Listen(IPAddress.Loopback, SSH_PORT, listen => listen.UseConnectionHandler<SshReverseProxyHandler>());
                kestrel.GetLogger().LogInformation($"已监听ssh://127.0.0.1:{SSH_PORT}，ssh反向代理到github启动完成");
            }
        }

        /// <summary>
        /// 尝试监听http反向代理
        /// </summary>
        /// <param name="kestrel"></param>
        public static void ListenHttpReverseProxy(this KestrelServerOptions kestrel)
        {
            const int HTTP_PORT = 80;
            if (LocalMachine.CanListenTcp(HTTP_PORT) == true)
            {
                kestrel.Listen(IPAddress.Loopback, HTTP_PORT);
                kestrel.GetLogger().LogInformation($"已监听http://127.0.0.1:{HTTP_PORT}，http反向代理启动完成");
            }
        }

        /// <summary>
        /// 监听https反向代理
        /// </summary>
        /// <param name="kestrel"></param>
        /// <exception cref="FastGithubException"></exception>
        /// <returns></returns>
        public static int ListenHttpsReverseProxy(this KestrelServerOptions kestrel)
        {
            var httpsPort = HttpsReverseProxyPort.Value;
            if (OperatingSystem.IsWindows())
            {
                TcpTable.KillPortOwner(httpsPort);
            }

            if (LocalMachine.CanListenTcp(httpsPort) == false)
            {
                throw new FastGithubException($"tcp端口{httpsPort}已经被其它进程占用");
            }

            var certService = kestrel.ApplicationServices.GetRequiredService<CertService>();
            certService.CreateCaCertIfNotExists();
            certService.InstallAndTrustCaCert();

            kestrel.Listen(IPAddress.Loopback, httpsPort,
                listen => listen.UseHttps(https =>
                    https.ServerCertificateSelector = (ctx, domain) =>
                        certService.GetOrCreateServerCert(domain)));

            if (httpsPort == 443)
            {
                var logger = kestrel.GetLogger();
                logger.LogInformation($"已监听https://127.0.0.1:{httpsPort}，https反向代理启动完成");
            }

            return httpsPort;
        }

        /// <summary>
        /// 获取日志
        /// </summary>
        /// <param name="kestrel"></param>
        /// <returns></returns>
        private static ILogger GetLogger(this KestrelServerOptions kestrel)
        {
            var loggerFactory = kestrel.ApplicationServices.GetRequiredService<ILoggerFactory>();
            return loggerFactory.CreateLogger($"{nameof(FastGithub)}.{nameof(ReverseProxy)}");
        }
    }
}
