using FastGithub.ReverseProxy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System;
using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;

namespace FastGithub
{
    /// <summary>
    /// ListenOptions扩展
    /// </summary>
    public static class ListenOptionsHttpsExtensions
    {
        /// <summary>
        /// 应用fastGihub的https
        /// </summary>
        /// <param name="listenOptions"></param>
        /// <param name="caPublicCerPath"></param>
        /// <param name="caPrivateKeyPath"></param>
        /// <returns></returns>
        public static ListenOptions UseGithubHttps(this ListenOptions listenOptions, string caPublicCerPath, string caPrivateKeyPath)
        {
            return listenOptions.UseHttps(https =>
            {
                var certs = new ConcurrentDictionary<string, X509Certificate2>();
                https.ServerCertificateSelector = (ctx, domain) => certs.GetOrAdd(domain, CreateCert);
            });

            X509Certificate2 CreateCert(string domain)
            {
                var domains = new[] { domain };
                var validFrom = DateTime.Today.AddYears(-1);
                var validTo = DateTime.Today.AddYears(10);
                return CertGenerator.Generate(domains, 2048, validFrom, validTo, caPublicCerPath, caPrivateKeyPath);
            }
        }
    }
}
