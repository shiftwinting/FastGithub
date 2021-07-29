using FastGithub.ReverseProxy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace FastGithub
{
    /// <summary>
    /// Kestrel扩展
    /// </summary>
    public static class KestrelServerOptionsExtensions
    {
        /// <summary>
        /// 服务器证书缓存
        /// </summary>
        private static readonly IMemoryCache serverCertCache = new MemoryCache(Options.Create(new MemoryCacheOptions()));

        /// <summary>
        /// 监听http的反向代理
        /// </summary>
        /// <param name="kestrel"></param>
        public static void ListenHttpReverseProxy(this KestrelServerOptions kestrel)
        {
            var loggerFactory = kestrel.ApplicationServices.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger($"{nameof(FastGithub)}.{nameof(ReverseProxy)}");

            const int HTTP_PORT = 80;
            if (OperatingSystem.IsWindows())
            {
                TcpTable.KillPortOwner(HTTP_PORT);
            }

            if (CanTcpListen(HTTP_PORT) == false)
            {
                logger.LogWarning($"无法监听tcp端口{HTTP_PORT}，{nameof(FastGithub)}无法http反向代理");
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
            var loggerFactory = kestrel.ApplicationServices.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger($"{nameof(FastGithub)}.{nameof(ReverseProxy)}");

            const string CAPATH = "CACert";
            Directory.CreateDirectory(CAPATH);
            var caPublicCerPath = $"{CAPATH}/{nameof(FastGithub)}.cer";
            var caPrivateKeyPath = $"{CAPATH}/{nameof(FastGithub)}.key";

            GeneratorCaCert(caPublicCerPath, caPrivateKeyPath);
            InstallCaCert(caPublicCerPath, logger);

            const int HTTPS_PORT = 443;
            if (OperatingSystem.IsWindows())
            {
                TcpTable.KillPortOwner(HTTPS_PORT);
            }

            if (CanTcpListen(HTTPS_PORT) == false)
            {
                logger.LogWarning($"无法监听tcp端口{HTTPS_PORT}，{nameof(FastGithub)}无法https反向代理");
            }
            else
            {
                kestrel.Listen(IPAddress.Any, HTTPS_PORT, listen =>
                    listen.UseHttps(https =>
                        https.ServerCertificateSelector = (ctx, domain) =>
                            GetServerCert(domain, caPublicCerPath, caPrivateKeyPath)));
            }
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

        /// <summary>
        /// 生成根证书
        /// 10年
        /// </summary>
        /// <param name="caPublicCerPath"></param>
        /// <param name="caPrivateKeyPath"></param>
        private static void GeneratorCaCert(string caPublicCerPath, string caPrivateKeyPath)
        {
            if (File.Exists(caPublicCerPath) && File.Exists(caPublicCerPath))
            {
                return;
            }

            File.Delete(caPublicCerPath);
            File.Delete(caPrivateKeyPath);

            var validFrom = DateTime.Today.AddDays(-1);
            var validTo = DateTime.Today.AddYears(10);
            CertGenerator.GenerateBySelf(new[] { nameof(FastGithub) }, 2048, validFrom, validTo, caPublicCerPath, caPrivateKeyPath);
        }


        /// <summary>
        /// 安装根证书
        /// </summary>
        /// <param name="caPublicCerPath"></param>
        /// <param name="logger"></param>
        private static void InstallCaCert(string caPublicCerPath, ILogger logger)
        {
            if (OperatingSystem.IsWindows() == false)
            {
                logger.LogWarning($"不支持自动安装证书{caPublicCerPath}：请手动安装证书到根证书颁发机构");
                return;
            }

            try
            {
                var caCert = new X509Certificate2(caPublicCerPath);
                using var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadWrite);
                if (store.Certificates.Find(X509FindType.FindByThumbprint, caCert.Thumbprint, true).Count == 0)
                {
                    store.Add(caCert);
                    store.Close();
                }
            }
            catch (Exception)
            {
                logger.LogWarning($"安装证书{caPublicCerPath}失败：请手动安装到“将所有的证书都放入下载存储”\\“受信任的根证书颁发机构”");
            }
        }

        /// <summary>
        /// 获取颁发给指定域名的证书
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="caPublicCerPath"></param>
        /// <param name="caPrivateKeyPath"></param>
        /// <returns></returns>
        private static X509Certificate2 GetServerCert(string? domain, string caPublicCerPath, string caPrivateKeyPath)
        {
            return serverCertCache.GetOrCreate(domain ?? string.Empty, GetOrCreateCert);

            // 生成域名的1年证书
            X509Certificate2 GetOrCreateCert(ICacheEntry entry)
            {
                var host = (string)entry.Key;
                var domains = GetDomains(host).Distinct();
                var validFrom = DateTime.Today.AddDays(-1);
                var validTo = DateTime.Today.AddYears(1);

                entry.SetAbsoluteExpiration(validTo);
                return CertGenerator.GenerateByCa(domains, 2048, validFrom, validTo, caPublicCerPath, caPrivateKeyPath);
            }
        }

        /// <summary>
        /// 获取域名
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        private static IEnumerable<string> GetDomains(string host)
        {
            if (string.IsNullOrEmpty(host) == false)
            {
                yield return host;
                yield break;
            }

            var globalPropreties = IPGlobalProperties.GetIPGlobalProperties();
            if (string.IsNullOrEmpty(globalPropreties.HostName) == false)
            {
                yield return globalPropreties.HostName;
            }

            foreach (var item in globalPropreties.GetUnicastAddresses())
            {
                if (item.Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    yield return item.Address.ToString();
                }
            }
        }
    }
}
