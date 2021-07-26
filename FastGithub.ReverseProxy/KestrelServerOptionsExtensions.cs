using FastGithub.ReverseProxy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
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
        /// 监听https的反向代理
        /// </summary>
        /// <param name="kestrel"></param>
        public static void ListenHttpsReverseProxy(this KestrelServerOptions kestrel)
        {
            var loggerFactory = kestrel.ApplicationServices.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger($"{nameof(FastGithub)}.{nameof(ReverseProxy)}");

            const string CAPATH = "CACert";
            Directory.CreateDirectory(CAPATH);
            var caPublicCerPath = $"{CAPATH}/{Environment.MachineName}.cer";
            var caPrivateKeyPath = $"{CAPATH}/{Environment.MachineName}.key";

            GeneratorCaCert(caPublicCerPath, caPrivateKeyPath);
            InstallCaCert(caPublicCerPath, logger);

            kestrel.Listen(IPAddress.Any, 443, listen =>
                listen.UseHttps(https =>
                    https.ServerCertificateSelector = (ctx, domain) =>
                        GetDomainCert(domain, caPublicCerPath, caPrivateKeyPath)));
        }

        /// <summary>
        /// 生成根证书
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

            var validFrom = DateTime.Today.AddYears(-10);
            var validTo = DateTime.Today.AddYears(50);
            CertGenerator.GenerateBySelf(new[] { nameof(FastGithub) }, 2048, validFrom, validTo, caPublicCerPath, caPrivateKeyPath);
        }


        /// <summary>
        /// 安装根证书
        /// </summary>
        /// <param name="caPublicCerPath"></param>
        /// <param name="logger"></param>
        private static void InstallCaCert(string caPublicCerPath, ILogger logger)
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
            catch (Exception)
            {
                if (OperatingSystem.IsWindows())
                {
                    logger.LogWarning($"安装根证书{caPublicCerPath}失败：请手动安装到“将所有的证书都放入下载存储”\\“受信任的根证书颁发机构”");
                }
                else
                {
                    logger.LogWarning($"安装根证书{caPublicCerPath}失败：请根据你的系统平台要求安装和信任根证书");
                }
            }
        }

        /// <summary>
        /// 获取颁发给指定域名的证书
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="caPublicCerPath"></param>
        /// <param name="caPrivateKeyPath"></param>
        /// <returns></returns>
        private static X509Certificate2 GetDomainCert(string? domain, string caPublicCerPath, string caPrivateKeyPath)
        {
            return domainCerts.GetOrAdd(domain ?? string.Empty, GetOrCreateCert).Value;

            Lazy<X509Certificate2> GetOrCreateCert(string host)
            {
                return new Lazy<X509Certificate2>(() =>
                {
                    var domains = GetDomains(host).Distinct();
                    var validFrom = DateTime.Today.AddYears(-1);
                    var validTo = DateTime.Today.AddYears(10);
                    return CertGenerator.GenerateByCa(domains, 2048, validFrom, validTo, caPublicCerPath, caPrivateKeyPath);
                }, LazyThreadSafetyMode.ExecutionAndPublication);
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
            }

            yield return Environment.MachineName;
            yield return IPAddress.Loopback.ToString();

            foreach (var @interface in NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (var addressInfo in @interface.GetIPProperties().UnicastAddresses)
                {
                    if (addressInfo.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        yield return addressInfo.Address.ToString();
                    }
                }
            }
        }
    }
}
