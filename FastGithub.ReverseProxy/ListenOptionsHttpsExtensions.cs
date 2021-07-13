using FastGithub.ReverseProxy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System;
using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;

namespace FastGithub
{
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
                https.ServerCertificateSelector = (ctx, domain) =>
                    certs.GetOrAdd(domain, d =>
                        CertGenerator.Generate(
                            new[] { d },
                            2048,
                            DateTime.Today.AddYears(-1),
                            DateTime.Today.AddYears(1),
                            caPublicCerPath,
                            caPrivateKeyPath));
            });
        }
    }
}
