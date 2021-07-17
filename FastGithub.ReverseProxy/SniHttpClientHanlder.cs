using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.ReverseProxy
{
    /// <summary>
    /// 携带Sni的的HttpClientHandler
    /// </summary> 
    class SniHttpClientHanlder : DelegatingHandler
    {
        private readonly DomainResolver domainResolver;
        private readonly ILogger<SniHttpClientHanlder> logger;

        /// <summary>
        /// 携带Sni的HttpClientHandler
        /// </summary>
        /// <param name="domainResolver"></param> 
        public SniHttpClientHanlder(
            DomainResolver domainResolver,
            ILogger<SniHttpClientHanlder> logger)
        {
            this.domainResolver = domainResolver;
            this.logger = logger;

            this.InnerHandler = new SocketsHttpHandler
            {
                Proxy = null,
                UseProxy = false,
                AllowAutoRedirect = false,
            };
        }

        /// <summary>
        /// 替换域名为ip
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri;
            if (uri != null && uri.HostNameType == UriHostNameType.Dns)
            {
                var address = await this.domainResolver.ResolveAsync(uri.Host, cancellationToken);
                this.logger.LogInformation($"[{address}--Sni->{uri.Host}]");

                var builder = new UriBuilder(uri)
                {
                    Host = address.ToString()
                };
                request.RequestUri = builder.Uri;
                request.Headers.Host = uri.Host;
            }
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
