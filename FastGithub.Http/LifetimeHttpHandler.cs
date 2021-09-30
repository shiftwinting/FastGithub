using FastGithub.Configuration;
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

        /// <summary>
        /// 获取域名配置
        /// </summary>
        public DomainConfig DomainConfig { get; }

        /// <summary>
        /// 具有生命周期的HttpHandler
        /// </summary>
        /// <param name="handler">HttpHandler</param>
        /// <param name="lifeTime">拦截器的生命周期</param>
        /// <param name="deactivateAction">失效回调</param>
        public LifetimeHttpHandler(HttpClientHandler handler, TimeSpan lifeTime, Action<LifetimeHttpHandler> deactivateAction)
            : base(handler)
        {
            this.DomainConfig = handler.DomainConfig;
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
