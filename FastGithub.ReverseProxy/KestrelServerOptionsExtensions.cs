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
        /// 监听http的反向代理
        /// </summary>
        /// <param name="kestrel"></param>
        public static void ListenHttpReverseProxy(this KestrelServerOptions kestrel)
        {
            const int HTTP_PORT = 80;
            var logger = kestrel.GetLogger();

            if (LocalMachine.CanListenTcp(HTTP_PORT) == false)
            {
                logger.LogWarning($"由于tcp端口{HTTP_PORT}已经被其它进程占用，http反向代理功能将受限");
            }
            else
            {
                kestrel.Listen(IPAddress.Any, HTTP_PORT);
                logger.LogInformation($"已监听http反向代理，访问 http://127.0.0.1 或本机其它任意ip可进入Dashboard");
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

            if (LocalMachine.CanListenTcp(HTTPS_PORT) == false)
            {
                throw new FastGithubException($"由于tcp端口{HTTPS_PORT}已经被其它进程占用，{nameof(FastGithub)}无法进行必须的https反向代理");
            }

            var certService = kestrel.ApplicationServices.GetRequiredService<CertService>();
            certService.CreateCaCertIfNotExists();
            certService.InstallAndTrustCaCert();

            kestrel.Listen(IPAddress.Any, HTTPS_PORT,
                listen => listen.UseHttps(https =>
                    https.ServerCertificateSelector = (ctx, domain) =>
                        certService.GetOrCreateServerCert(domain)));

            var logger = kestrel.GetLogger();
            logger.LogInformation($"已监听https反向代理，访问 https://127.0.0.1 或本机其它任意ip可进入Dashboard");
        }

        /// <summary>
        /// 监听github的ssh的代理
        /// </summary>
        /// <param name="kestrel"></param>
        public static void ListenGithubSshProxy(this KestrelServerOptions kestrel)
        {
            var listenOptions = kestrel.ApplicationServices.GetRequiredService<IOptions<FastGithubOptions>>();
            var sshPort = listenOptions.Value.Listen.SshPort;
            var logger = kestrel.GetLogger();

            if (LocalMachine.CanListenTcp(sshPort) == false)
            {
                logger.LogWarning($"由于tcp端口{sshPort}已经被其它进程占用，github的ssh代理功能将受限");
            }
            else
            {
                kestrel.Listen(IPAddress.Any, sshPort, listen => listen.UseConnectionHandler<GithubSshHandler>());
                logger.LogInformation("已监听github的ssh代理");
            }
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
