using FastGithub.ReverseProxy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace FastGithub
{
    /// <summary>
    /// Kestrel扩展
    /// </summary>
    public static class KestrelServerOptionsExtensions
    {
        /// <summary>
        /// 域名与证书
        /// </summary>
        private static readonly ConcurrentDictionary<string, Lazy<X509Certificate2>> domainCerts = new();

        /// <summary>
        /// 监听github的反向代理
        /// </summary>
        /// <param name="kestrel"></param>
        /// <param name="caPublicCerPath"></param>
        /// <param name="caPrivateKeyPath"></param>
        public static void ListenGithubReverseProxy(this KestrelServerOptions kestrel, string caPublicCerPath, string caPrivateKeyPath)
        {
            var loggerFactory = kestrel.ApplicationServices.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger($"{nameof(FastGithub)}{nameof(ReverseProxy)}");
            TryInstallCaCert(caPublicCerPath, logger);

            kestrel.ListenAnyIP(443, listen =>
                listen.UseHttps(https =>
                    https.ServerCertificateSelector = (ctx, domain) =>
                        GetOrCreateCert(domain)));

            logger.LogInformation("反向代理服务启动成功");


            X509Certificate2 GetOrCreateCert(string key)
            {
                if (key == string.Empty)
                {
                    key = "github.com";
                }

                return domainCerts.GetOrAdd(key, domain => new Lazy<X509Certificate2>(() =>
                {
                    var domains = new[] { domain };
                    var validFrom = DateTime.Today.AddYears(-1);
                    var validTo = DateTime.Today.AddYears(10);
                    return CertGenerator.Generate(domains, 2048, validFrom, validTo, caPublicCerPath, caPrivateKeyPath);
                }, LazyThreadSafetyMode.ExecutionAndPublication)).Value;
            }
        }

        /// <summary>
        /// 安装根证书
        /// </summary>
        /// <param name="caPublicCerPath"></param>
        /// <param name="logger"></param>
        private static void TryInstallCaCert(string caPublicCerPath, ILogger logger)
        {
            if (OperatingSystem.IsWindows())
            {
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
                catch (Exception  )
                {
                    logger.LogError($"安装根证书{caPublicCerPath}失败：请手动安装到“将所有的证书都放入下载存储”\\“受信任的根证书颁发机构”");
                }
            }
        }

    }
}
