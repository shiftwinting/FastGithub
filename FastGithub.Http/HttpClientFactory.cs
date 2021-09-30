using FastGithub.Configuration;
using FastGithub.DomainResolve;
using System;
using System.Collections.Concurrent;

namespace FastGithub.Http
{
    /// <summary>
    /// HttpClient工厂
    /// </summary>
    sealed class HttpClientFactory : IHttpClientFactory
    {
        private readonly IDomainResolver domainResolver;

        /// <summary>
        /// httpHandler的生命周期
        /// </summary>
        private readonly TimeSpan lifeTime = TimeSpan.FromMinutes(1d);

        /// <summary>
        /// HttpHandler清理器
        /// </summary>
        private readonly LifetimeHttpHandlerCleaner httpHandlerCleaner = new();

        /// <summary>
        /// LazyOf(LifetimeHttpHandler)缓存
        /// </summary>
        private readonly ConcurrentDictionary<DomainConfig, Lazy<LifetimeHttpHandler>> httpHandlerLazyCache = new();

        /// <summary>
        /// HttpClient工厂
        /// </summary>
        /// <param name="domainResolver"></param>
        public HttpClientFactory(IDomainResolver domainResolver)
        {
            this.domainResolver = domainResolver;
        }

        /// <summary>
        /// 创建httpClient
        /// </summary>
        /// <param name="domainConfig"></param>
        /// <returns></returns>
        public HttpClient CreateHttpClient(DomainConfig domainConfig)
        {
            var lifetimeHttpHandlerLazy = this.httpHandlerLazyCache.GetOrAdd(domainConfig, this.CreateLifetimeHttpHandlerLazy);
            var lifetimeHttpHandler = lifetimeHttpHandlerLazy.Value;
            return new HttpClient(lifetimeHttpHandler, disposeHandler: false);
        }

        /// <summary>
        /// 创建LazyOf(LifetimeHttpHandler)
        /// </summary>
        /// <param name="domainConfig"></param>
        /// <returns></returns>
        private Lazy<LifetimeHttpHandler> CreateLifetimeHttpHandlerLazy(DomainConfig domainConfig)
        {
            return new Lazy<LifetimeHttpHandler>(() => this.CreateLifetimeHttpHandler(domainConfig), true);
        }

        /// <summary>
        /// 创建LifetimeHttpHandler
        /// </summary>
        /// <returns></returns>
        private LifetimeHttpHandler CreateLifetimeHttpHandler(DomainConfig domainConfig)
        {
            var httpClientHandler = new HttpClientHandler(domainConfig, this.domainResolver);
            return new LifetimeHttpHandler(httpClientHandler, this.lifeTime, this.OnLifetimeHttpHandlerDeactivate);
        }

        /// <summary>
        /// 当有httpHandler失效时
        /// </summary>
        /// <param name="lifetimeHttpHandler">httpHandler</param>
        private void OnLifetimeHttpHandlerDeactivate(LifetimeHttpHandler lifetimeHttpHandler)
        {
            // 切换激活状态的记录的实例
            var domainConfig = lifetimeHttpHandler.DomainConfig;
            this.httpHandlerLazyCache[domainConfig] = this.CreateLifetimeHttpHandlerLazy(domainConfig);
            this.httpHandlerCleaner.Add(lifetimeHttpHandler);
        }
    }
}