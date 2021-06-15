using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FastGithub
{
    /// <summary>
    /// 表示中间件创建者
    /// </summary>
    sealed class GithubScanBuilder : IGithubScanBuilder
    {
        private readonly GithubScanDelegate completedHandler;
        private readonly List<Func<GithubScanDelegate, GithubScanDelegate>> middlewares = new();

        /// <summary>
        /// 获取服务提供者
        /// </summary>
        public IServiceProvider AppServices { get; }

        /// <summary>
        /// 中间件创建者
        /// </summary>
        /// <param name="appServices"></param>
        public GithubScanBuilder(IServiceProvider appServices)
            : this(appServices, context => Task.CompletedTask)
        {
        }

        /// <summary>
        /// 中间件创建者
        /// </summary>
        /// <param name="appServices"></param>
        /// <param name="completedHandler">完成执行内容处理者</param>
        public GithubScanBuilder(IServiceProvider appServices, GithubScanDelegate completedHandler)
        {
            this.AppServices = appServices;
            this.completedHandler = completedHandler;
        }

        /// <summary>
        /// 使用中间件
        /// </summary>
        /// <param name="middleware"></param>
        /// <returns></returns>
        public IGithubScanBuilder Use(Func<GithubScanDelegate, GithubScanDelegate> middleware)
        {
            this.middlewares.Add(middleware);
            return this;
        }


        /// <summary>
        /// 创建所有中间件执行处理者
        /// </summary>
        /// <returns></returns>
        public GithubScanDelegate Build()
        {
            var handler = this.completedHandler;
            for (var i = this.middlewares.Count - 1; i >= 0; i--)
            {
                handler = this.middlewares[i](handler);
            }
            return handler;
        }
    }
}