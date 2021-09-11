using FastGithub.Configuration;
using FastGithub.ReverseProxy;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        /// 监听ssh
        /// </summary>
        /// <param name="kestrel"></param>
        public static void ListenSsh(this KestrelServerOptions kestrel)
        {
            const int SSH_PORT = 22;
            if (LocalMachine.CanListenTcp(SSH_PORT) == true)
            {
                kestrel.Listen(IPAddress.Any, SSH_PORT, listen => listen.UseConnectionHandler<GithubSshProxyHandler>());
                kestrel.GetLogger().LogInformation($"已监听tcp端口{SSH_PORT}，github的ssh代理启动完成");
            }
        }

        /// <summary>
        /// 监听http
        /// </summary>
        /// <param name="kestrel"></param>
        public static void ListenHttp(this KestrelServerOptions kestrel)
        {
            const int HTTP_PORT = 80;
            if (LocalMachine.CanListenTcp(HTTP_PORT) == true)
            {
                kestrel.Listen(IPAddress.Any, HTTP_PORT);
                kestrel.GetLogger().LogInformation($"已监听tcp端口{HTTP_PORT}，http反向代理启动完成");
            }
        }

        /// <summary>
        /// 监听https
        /// </summary>
        /// <param name="kestrel"></param>
        /// <exception cref="FastGithubException"></exception>
        public static void ListenHttps(this KestrelServerOptions kestrel)
        {
            const int HTTPS_PORT = 443;
            if (OperatingSystem.IsWindows())
            {
                TcpTable.KillPortOwner(HTTPS_PORT);
            }

            if (LocalMachine.CanListenTcp(HTTPS_PORT) == false)
            {
                throw new FastGithubException($"tcp端口{HTTPS_PORT}已经被其它进程占用");
            }

            var certService = kestrel.ApplicationServices.GetRequiredService<CertService>();
            certService.CreateCaCertIfNotExists();
            certService.InstallAndTrustCaCert();

            kestrel.Listen(IPAddress.Any, HTTPS_PORT,
                listen => listen.UseHttps(https =>
                    https.ServerCertificateSelector = (ctx, domain) =>
                        certService.GetOrCreateServerCert(domain)));

            var logger = kestrel.GetLogger();
            logger.LogInformation($"已监听tcp端口{HTTPS_PORT}，https反向代理启动完成");
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
