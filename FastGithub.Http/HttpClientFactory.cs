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
        /// 首次生命周期
        /// </summary>
        private readonly TimeSpan firstLiftTime = TimeSpan.FromSeconds(10d);

        /// <summary>
        /// 非首次生命周期
        /// </summary>
        private readonly TimeSpan nextLifeTime = TimeSpan.FromMinutes(1d);

        /// <summary>
        /// LifetimeHttpHandler清理器
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
            var lifetimeHttpHandlerLazy = this.httpHandlerLazyCache.GetOrAdd(domainConfig, CreateLifetimeHttpHandlerLazy);
            var lifetimeHttpHandler = lifetimeHttpHandlerLazy.Value;
            return new HttpClient(lifetimeHttpHandler, disposeHandler: false);

            Lazy<LifetimeHttpHandler> CreateLifetimeHttpHandlerLazy(DomainConfig domainConfig)
            {
                return new Lazy<LifetimeHttpHandler>(() => this.CreateLifetimeHttpHandler(domainConfig, this.firstLiftTime), true);
            }
        }


        /// <summary>
        /// 当有httpHandler失效时
        /// </summary>
        /// <param name="lifetimeHttpHandler">httpHandler</param>
        private void OnLifetimeHttpHandlerDeactivate(LifetimeHttpHandler lifetimeHttpHandler)
        {
            var domainConfig = lifetimeHttpHandler.DomainConfig;
            this.httpHandlerLazyCache[domainConfig] = CreateLifetimeHttpHandlerLazy(domainConfig);
            this.httpHandlerCleaner.Add(lifetimeHttpHandler);

            Lazy<LifetimeHttpHandler> CreateLifetimeHttpHandlerLazy(DomainConfig domainConfig)
            {
                return new Lazy<LifetimeHttpHandler>(() => this.CreateLifetimeHttpHandler(domainConfig, this.nextLifeTime), true);
            }
        }

        /// <summary>
        /// 创建LifetimeHttpHandler
        /// </summary>
        /// <param name="domainConfig"></param>
        /// <param name="lifeTime"></param>
        /// <returns></returns>
        private LifetimeHttpHandler CreateLifetimeHttpHandler(DomainConfig domainConfig, TimeSpan lifeTime)
        {
            var httpClientHandler = new HttpClientHandler(domainConfig, this.domainResolver);
            return new LifetimeHttpHandler(httpClientHandler, lifeTime, this.OnLifetimeHttpHandlerDeactivate);
        }
    }
}