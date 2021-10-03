using FastGithub.DomainResolve;
using System;
using System.Net.Http;
using System.Threading;

namespace FastGithub.Http
{
    /// <summary>
    /// 表示自主管理生命周期的的HttpMessageHandler
    /// </summary>
    sealed class LifetimeHttpHandler : DelegatingHandler
    {
        private readonly Timer timer;

        public LifeTimeKey LifeTimeKey { get; }

        /// <summary>
        /// 具有生命周期的HttpHandler
        /// </summary>
        /// <param name="domainResolver"></param>
        /// <param name="lifeTimeKey"></param>
        /// <param name="lifeTime"></param>
        /// <param name="deactivateAction"></param>
        public LifetimeHttpHandler(IDomainResolver domainResolver, LifeTimeKey lifeTimeKey, TimeSpan lifeTime, Action<LifetimeHttpHandler> deactivateAction)
        {
            this.LifeTimeKey = lifeTimeKey;
            this.InnerHandler = new HttpClientHandler(lifeTimeKey.DomainConfig, domainResolver);
            this.timer = new Timer(this.OnTimerCallback, deactivateAction, lifeTime, Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// timer触发时
        /// </summary>
        /// <param name="state"></param>
        private void OnTimerCallback(object? state)
        {
            this.timer.Dispose();
            ((Action<LifetimeHttpHandler>)(state!))(this);
        }

        /// <summary>
        /// 这里不释放资源
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
        }
    }
}
