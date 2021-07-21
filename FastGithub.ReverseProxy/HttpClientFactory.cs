using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.ReverseProxy
{
    /// <summary>
    /// HttpClient工厂
    /// </summary>
    sealed class HttpClientFactory
    {
        private HttpClientHanlder httpClientHanlder;

        /// <summary>
        /// HttpClient工厂
        /// </summary>
        /// <param name="domainResolver"></param>
        /// <param name="options"></param>
        public HttpClientFactory(
            DomainResolver domainResolver,
            IOptionsMonitor<FastGithubOptions> options)
        {
            this.httpClientHanlder = new HttpClientHanlder(domainResolver);
            options.OnChange(opt => this.httpClientHanlder = new HttpClientHanlder(domainResolver));
        }

        /// <summary>
        /// 创建httpClient
        /// </summary>
        /// <param name="domainConfig"></param>
        /// <returns></returns>
        public HttpMessageInvoker CreateHttpClient(DomainConfig domainConfig)
        {
            return new HttpClient(this.httpClientHanlder, domainConfig, disposeHandler: false);
        }

        /// <summary>
        /// http客户端
        /// </summary>
        private class HttpClient : HttpMessageInvoker
        {
            private readonly DomainConfig domainConfig;

            public HttpClient(
                HttpMessageHandler handler,
                DomainConfig domainConfig,
                bool disposeHandler = false) : base(handler, disposeHandler)
            {
                this.domainConfig = domainConfig;
            }

            /// <summary>
            /// 发送数据
            /// </summary>
            /// <param name="request"></param>
            /// <param name="cancellationToken"></param>
            /// <returns></returns>
            public override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                request.SetRequestContext(new RequestContext
                {
                    Host = request.RequestUri?.Host,
                    IsHttps = request.RequestUri?.Scheme == Uri.UriSchemeHttps,
                    TlsSniPattern = this.domainConfig.GetTlsSniPattern(),
                    TlsIgnoreNameMismatch = this.domainConfig.TlsIgnoreNameMismatch
                });
                return base.SendAsync(request, cancellationToken);
            }
        }
    }
}
