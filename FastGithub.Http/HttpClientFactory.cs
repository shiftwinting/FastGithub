using FastGithub.Configuration;
using FastGithub.DomainResolve;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace FastGithub.Http
{
    /// <summary>
    /// HttpClient工厂
    /// </summary>
    sealed class HttpClientFactory : IHttpClientFactory
    {
        private readonly IDomainResolver domainResolver;
        private ConcurrentDictionary<DomainConfig, HttpClientHandler> domainHandlers = new();

        /// <summary>
        /// HttpClient工厂
        /// </summary>
        /// <param name="domainResolver"></param>
        /// <param name="options"></param>
        public HttpClientFactory(
            IDomainResolver domainResolver,
            IOptionsMonitor<FastGithubOptions> options)
        {
            this.domainResolver = domainResolver;
            options.OnChange(opt => this.domainHandlers = new());
        }

        /// <summary>
        /// 创建httpClient
        /// </summary>
        /// <param name="domainConfig"></param>
        /// <returns></returns>
        public HttpClient CreateHttpClient(DomainConfig domainConfig)
        {
            var httpClientHandler = this.domainHandlers.GetOrAdd(domainConfig, config => new HttpClientHandler(config, this.domainResolver));
            return new HttpClient(httpClientHandler, disposeHandler: false);
        }
    }
}