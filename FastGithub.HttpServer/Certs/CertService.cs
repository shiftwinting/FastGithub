using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace FastGithub.HttpServer.Certs
{
    /// <summary>
    /// 证书服务
    /// </summary>
    sealed class CertService
    {
        private const string CACERT_PATH = "cacert";
        private readonly IMemoryCache serverCertCache;
        private readonly IEnumerable<ICaCertInstaller> certInstallers;
        private readonly ILogger<CertService> logger;
        private X509Certificate2? caCert;


        /// <summary>
        /// 获取证书文件路径
        /// </summary>
        public string CaCerFilePath { get; } = OperatingSystem.IsLinux() ? $"{CACERT_PATH}/fastgithub.crt" : $"{CACERT_PATH}/fastgithub.cer";

        /// <summary>
        /// 获取私钥文件路径
        /// </summary>
        public string CaKeyFilePath { get; } = $"{CACERT_PATH}/fastgithub.key";

        /// <summary>
        /// 证书服务
        /// </summary>
        /// <param name="serverCertCache"></param>
        /// <param name="certInstallers"></param>
        /// <param name="logger"></param>
        public CertService(
            IMemoryCache serverCertCache,
            IEnumerable<ICaCertInstaller> certInstallers,
            ILogger<CertService> logger)
        {
            this.serverCertCache = serverCertCache;
            this.certInstallers = certInstallers;
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

            var notBefore = DateTimeOffset.Now.AddDays(-1);
            var notAfter = DateTimeOffset.Now.AddYears(10);

            var subjectName = new X500DistinguishedName($"CN={nameof(FastGithub)}");
            this.caCert = CertGenerator.CreateCACertificate(subjectName, notBefore, notAfter);

            var privateKeyPem = this.caCert.GetRSAPrivateKey()?.ExportRSAPrivateKeyPem();
            File.WriteAllText(this.CaKeyFilePath, new string(privateKeyPem), Encoding.ASCII);

            var certPem = this.caCert.ExportCertificatePem();
            File.WriteAllText(this.CaCerFilePath, new string(certPem), Encoding.ASCII);

            return true;
        }

        /// <summary>
        /// 安装和信任CA证书
        /// </summary> 
        public void InstallAndTrustCaCert()
        {
            var installer = this.certInstallers.FirstOrDefault(item => item.IsSupported());
            if (installer != null)
            {
                installer.Install(this.CaCerFilePath);
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
        /// 获取颁发给指定域名的证书
        /// </summary>
        /// <param name="domain"></param> 
        /// <returns></returns>
        public X509Certificate2 GetOrCreateServerCert(string? domain)
        {
            if (this.caCert == null)
            {
                using var rsa = RSA.Create();
                rsa.ImportFromPem(File.ReadAllText(this.CaKeyFilePath));
                this.caCert = new X509Certificate2(this.CaCerFilePath).CopyWithPrivateKey(rsa);
            }

            var key = $"{nameof(CertService)}:{domain}";
            var endCert = this.serverCertCache.GetOrCreate(key, GetOrCreateCert);
            return endCert!;

            // 生成域名的1年证书
            X509Certificate2 GetOrCreateCert(ICacheEntry entry)
            {
                var notBefore = DateTimeOffset.Now.AddDays(-1);
                var notAfter = DateTimeOffset.Now.AddYears(1);
                entry.SetAbsoluteExpiration(notAfter);

                var extraDomains = GetExtraDomains();

                var subjectName = new X500DistinguishedName($"CN={domain}");
                var endCert = CertGenerator.CreateEndCertificate(this.caCert, subjectName, extraDomains, notBefore, notAfter);

                // 重新初始化证书，以兼容win平台不能使用内存证书
                return new X509Certificate2(endCert.Export(X509ContentType.Pfx));
            }
        }

        /// <summary>
        /// 获取域名
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        private static IEnumerable<string> GetExtraDomains()
        {
            yield return Environment.MachineName;
            yield return IPAddress.Loopback.ToString();
            yield return IPAddress.IPv6Loopback.ToString();
        }
    }
}
