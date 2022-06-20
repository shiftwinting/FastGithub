using Microsoft.AspNetCore.Http.Features;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.HttpServer.TlsMiddlewares
{
    /// <summary>
    /// 假冒的TlsConnectionFeature
    /// </summary>
    sealed class FakeTlsConnectionFeature : ITlsConnectionFeature
    {
        public static FakeTlsConnectionFeature Instance { get; } = new FakeTlsConnectionFeature();

        public X509Certificate2? ClientCertificate
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public Task<X509Certificate2?> GetClientCertificateAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
