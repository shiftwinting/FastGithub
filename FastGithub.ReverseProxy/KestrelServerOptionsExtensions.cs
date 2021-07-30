using FastGithub.Configuration;
using FastGithub.ReverseProxy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Authentication;

namespace FastGithub
{
    /// <summary>
    /// Kestrel扩展
    /// </summary>
    public static class KestrelServerOptionsExtensions
    {
        /// <summary>
        /// 监听http的反向代理
        /// </summary>
        /// <param name="kestrel"></param>
        public static void ListenHttpReverseProxy(this KestrelServerOptions kestrel)
        {
            const int HTTP_PORT = 80;
            if (OperatingSystem.IsWindows())
            {
                TcpTable.KillPortOwner(HTTP_PORT);
            }

            if (CanTcpListen(HTTP_PORT) == false)
            {
                var loggerFactory = kestrel.ApplicationServices.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger($"{nameof(FastGithub)}.{nameof(ReverseProxy)}");
                logger.LogWarning($"由于tcp端口{HTTP_PORT}已经被其它进程占用，http反向代理功能将受限");
            }
            else
            {
                kestrel.Listen(IPAddress.Any, HTTP_PORT);
            }
        }

        /// <summary>
        /// 监听https的反向代理
        /// </summary>
        /// <param name="kestrel"></param>
        public static void ListenHttpsReverseProxy(this KestrelServerOptions kestrel)
        {
            const int HTTPS_PORT = 443;
            if (OperatingSystem.IsWindows())
            {
                TcpTable.KillPortOwner(HTTPS_PORT);
            }

            if (CanTcpListen(HTTPS_PORT) == false)
            {
                throw new FastGithubException($"由于tcp端口{HTTPS_PORT}已经被其它进程占用，{nameof(FastGithub)}无法进行必须的https反向代理");
            }

            var certService = kestrel.ApplicationServices.GetRequiredService<CertService>();
            certService.CreateCaCertIfNotExists();
            certService.InstallAndTrustCaCert();

            kestrel.Listen(IPAddress.Any, HTTPS_PORT, listen => listen.UseHttps(https =>
            {
                https.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
                https.ServerCertificateSelector = (ctx, domain) => certService.GetOrCreateServerCert(domain);
            }));
        }

        /// <summary>
        /// 是否可以监听指定端口
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        private static bool CanTcpListen(int port)
        {
            var tcpListeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
            return tcpListeners.Any(item => item.Port == port) == false;
        }
    }
}
