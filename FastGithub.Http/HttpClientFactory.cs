using FastGithub.DomainResolve;
using Microsoft.Extensions.Options;

namespace FastGithub.Http
{
    /// <summary>
    /// HttpClient工厂
    /// </summary>
    sealed class HttpClientFactory : IHttpClientFactory
    {
        private HttpClientHandler httpClientHanlder;

        /// <summary>
        /// HttpClient工厂
        /// </summary>
        /// <param name="domainResolver"></param>
        /// <param name="options"></param>
        public HttpClientFactory(
            IDomainResolver domainResolver,
            IOptionsMonitor<FastGithubOptions> options)
        {
            this.httpClientHanlder = new HttpClientHandler(domainResolver);
            options.OnChange(opt => this.httpClientHanlder = new HttpClientHandler(domainResolver));
        }

        /// <summary>
        /// 创建httpClient
        /// </summary>
        /// <param name="domainConfig"></param>
        /// <returns></returns>
        public HttpClient CreateHttpClient(DomainConfig domainConfig)
        {
            return new HttpClient(domainConfig, this.httpClientHanlder, disposeHandler: false);
        }
    }
}