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
        private readonly TimeSpan nextLifeTime = TimeSpan.FromSeconds(100d);

        /// <summary>
        /// LifetimeHttpHandler清理器
        /// </summary>
        private readonly LifetimeHttpHandlerCleaner httpHandlerCleaner = new();

        /// <summary>
        /// LazyOf(LifetimeHttpHandler)缓存
        /// </summary>
        private readonly ConcurrentDictionary<LifeTimeKey, Lazy<LifetimeHttpHandler>> httpHandlerLazyCache = new();


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
        /// <param name="domain"></param>
        /// <param name="domainConfig"></param>
        /// <returns></returns>
        public HttpClient CreateHttpClient(string domain, DomainConfig domainConfig)
        {
            var lifeTimeKey = new LifeTimeKey(domain, domainConfig);
            var lifetimeHttpHandler = this.httpHandlerLazyCache.GetOrAdd(lifeTimeKey, CreateLifetimeHttpHandlerLazy).Value;
            return new HttpClient(lifetimeHttpHandler, disposeHandler: false);

            Lazy<LifetimeHttpHandler> CreateLifetimeHttpHandlerLazy(LifeTimeKey lifeTimeKey)
            {
                return new Lazy<LifetimeHttpHandler>(() => this.CreateLifetimeHttpHandler(lifeTimeKey, this.firstLiftTime), true);
            }
        }

        /// <summary>
        /// 创建LifetimeHttpHandler
        /// </summary>
        /// <param name="lifeTimeKey"></param>
        /// <param name="lifeTime"></param>
        /// <returns></returns>
        private LifetimeHttpHandler CreateLifetimeHttpHandler(LifeTimeKey lifeTimeKey, TimeSpan lifeTime)
        {
            return new LifetimeHttpHandler(this.domainResolver, lifeTimeKey, lifeTime, this.OnLifetimeHttpHandlerDeactivate);
        }

        /// <summary>
        /// 当有httpHandler失效时
        /// </summary>
        /// <param name="lifetimeHttpHandler">httpHandler</param>
        private void OnLifetimeHttpHandlerDeactivate(LifetimeHttpHandler lifetimeHttpHandler)
        {
            var lifeTimeKey = lifetimeHttpHandler.LifeTimeKey;
            this.httpHandlerLazyCache[lifeTimeKey] = CreateLifetimeHttpHandlerLazy(lifeTimeKey);
            this.httpHandlerCleaner.Add(lifetimeHttpHandler);

            Lazy<LifetimeHttpHandler> CreateLifetimeHttpHandlerLazy(LifeTimeKey lifeTimeKey)
            {
                return new Lazy<LifetimeHttpHandler>(() => this.CreateLifetimeHttpHandler(lifeTimeKey, this.nextLifeTime), true);
            }
        }
    }
}