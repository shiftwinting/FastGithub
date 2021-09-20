using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace FastGithub.HttpServer
{
    /// <summary>
    /// 证书服务
    /// </summary>
    sealed class CertService
    {
        private const string CACERT_PATH = "cacert";
        private const int KEY_SIZE_BITS = 2048;
        private readonly IMemoryCache serverCertCache;
        private readonly ILogger<CertService> logger;


        /// <summary>
        /// 获取证书文件路径
        /// </summary>
        public string CaCerFilePath { get; } = $"{CACERT_PATH}/fastgithub.cer";

        /// <summary>
        /// 获取私钥文件路径
        /// </summary>
        public string CaKeyFilePath { get; } = $"{CACERT_PATH}/fastgithub.key";

        /// <summary>
        /// 证书服务
        /// </summary>
        /// <param name="logger"></param>
        public CertService(
            IMemoryCache serverCertCache,
            ILogger<CertService> logger)
        {
            this.serverCertCache = serverCertCache;
            this.logger = logger;
            Directory.CreateDirectory(CACERT_PATH);
        }

        /// <summary>
        /// 生成CA证书
        /// </summary> 
        public bool CreateCaCertIfNotExists()
        {
            if (File.Exists(this.CaCerFilePath) && File.Exists(this.CaKeyFilePath))
            {
                return false;
            }

            File.Delete(this.CaCerFilePath);
            File.Delete(this.CaKeyFilePath);

            var validFrom = DateTime.Today.AddDays(-1);
            var validTo = DateTime.Today.AddYears(10);
            CertGenerator.GenerateBySelf(new[] { nameof(FastGithub) }, KEY_SIZE_BITS, validFrom, validTo, this.CaCerFilePath, this.CaKeyFilePath);
            return true;
        }

        /// <summary>
        /// 安装和信任CA证书
        /// </summary> 
        public void InstallAndTrustCaCert()
        {
            if (OperatingSystem.IsWindows())
            {
                this.InstallAndTrustCaCertAtWindows();
            }
            else if (OperatingSystem.IsLinux())
            {
                this.logger.LogWarning($"请根据具体linux发行版手动安装CA证书{this.CaCerFilePath}");
            }
            else if (OperatingSystem.IsMacOS())
            {
                this.logger.LogWarning($"请手动安装CA证书然后设置信任CA证书{this.CaCerFilePath}");
            }
            else
            {
                this.logger.LogWarning($"请根据你的系统平台手动安装和信任CA证书{this.CaCerFilePath}");
            }

            GitConfigSslverify(false);
        }

        /// <summary>
        /// 设置ssl验证
        /// </summary>
        /// <param name="value">是否验证</param>
        /// <returns></returns>
        public static bool GitConfigSslverify(bool value)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = $"config --global http.sslverify {value.ToString().ToLower()}",
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 安装CA证书
        /// </summary> 
        private void InstallAndTrustCaCertAtWindows()
        {
            try
            {
                using var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadWrite);

                var caCert = new X509Certificate2(this.CaCerFilePath);
                var subjectName = caCert.Subject[3..];
                foreach (var item in store.Certificates.Find(X509FindType.FindBySubjectName, subjectName, false))
                {
                    if (item.Thumbprint != caCert.Thumbprint)
                    {
                        store.Remove(item);
                    }
                }
                if (store.Certificates.Find(X509FindType.FindByThumbprint, caCert.Thumbprint, true).Count == 0)
                {
                    store.Add(caCert);
                }
                store.Close();
            }
            catch (Exception)
            {
                this.logger.LogWarning($"请手动安装CA证书{this.CaCerFilePath}到“将所有的证书都放入下列存储”\\“受信任的根证书颁发机构”");
            }
        }


        /// <summary>
        /// 获取颁发给指定域名的证书
        /// </summary>
        /// <param name="domain"></param> 
        /// <returns></returns>
        public X509Certificate2 GetOrCreateServerCert(string? domain)
        {
            var key = $"{nameof(CertService)}:{domain}";
            return this.serverCertCache.GetOrCreate(key, GetOrCreateCert);

            // 生成域名的1年证书
            X509Certificate2 GetOrCreateCert(ICacheEntry entry)
            {
                var domains = GetDomains(domain).Distinct();
                var validFrom = DateTime.Today.AddDays(-1);
                var validTo = DateTime.Today.AddYears(1);

                entry.SetAbsoluteExpiration(validTo);
                return CertGenerator.GenerateByCa(domains, KEY_SIZE_BITS, validFrom, validTo, this.CaCerFilePath, this.CaKeyFilePath);
            }
        }

        /// <summary>
        /// 获取域名
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        private static IEnumerable<string> GetDomains(string? domain)
        {
            if (string.IsNullOrEmpty(domain) == false)
            {
                yield return domain;
                yield break;
            }

            yield return Environment.MachineName;
            yield return IPAddress.Loopback.ToString();
        }
    }
}
