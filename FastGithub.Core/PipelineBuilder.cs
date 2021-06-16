using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FastGithub
{
    /// <summary>
    /// 表示中间件创建者
    /// </summary>
    public class PipelineBuilder<TContext> : IPipelineBuilder<TContext>
    {
        private readonly InvokeDelegate<TContext> completedHandler;
        private readonly List<Func<InvokeDelegate<TContext>, InvokeDelegate<TContext>>> middlewares = new List<Func<InvokeDelegate<TContext>, InvokeDelegate<TContext>>>();

        /// <summary>
        /// 获取服务提供者
        /// </summary>
        public IServiceProvider AppServices { get; }

        /// <summary>
        /// 中间件创建者
        /// </summary>
        /// <param name="appServices"></param>
        public PipelineBuilder(IServiceProvider appServices)
            : this(appServices, context => Task.CompletedTask)
        {
        }

        /// <summary>
        /// 中间件创建者
        /// </summary>
        /// <param name="appServices"></param>
        /// <param name="completedHandler">完成执行内容处理者</param>
        public PipelineBuilder(IServiceProvider appServices, InvokeDelegate<TContext> completedHandler)
        {
            this.AppServices = appServices;
            this.completedHandler = completedHandler;
        }


        /// <summary>
        /// 使用中间件
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <typeparam name="TMiddleware"></typeparam>
        /// <param name="builder"></param>
        /// <returns></returns>
        public IPipelineBuilder<TContext> Use<TMiddleware>() where TMiddleware : class, IMiddleware<TContext>
        {
            var middleware = this.AppServices.GetRequiredService<TMiddleware>();
            return this.Use(middleware.InvokeAsync);
        }

        /// <summary>
        /// 使用中间件
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <param name="builder"></param>
        /// <param name="middleware"></param>
        /// <returns></returns>
        public IPipelineBuilder<TContext> Use(Func<TContext, Func<Task>, Task> middleware)
        {
            return this.Use(next => context => middleware(context, () => next(context)));
        }

        /// <summary>
        /// 使用中间件
        /// </summary>
        /// <param name="middleware"></param>
        /// <returns></returns>
        public IPipelineBuilder<TContext> Use(Func<InvokeDelegate<TContext>, InvokeDelegate<TContext>> middleware)
        {
            this.middlewares.Add(middleware);
            return this;
        }


        /// <summary>
        /// 创建所有中间件执行处理者
        /// </summary>
        /// <returns></returns>
        public InvokeDelegate<TContext> Build()
        {
            var handler = this.completedHandler;
            for (var i = this.middlewares.Count - 1; i >= 0; i--)
            {
                handler = this.middlewares[i](handler);
            }
            return handler;
        }


        /// <summary>
        /// 使用默认配制创建新的PipelineBuilder
        /// </summary>
        /// <returns></returns>
        public IPipelineBuilder<TContext> New()
        {
            return new PipelineBuilder<TContext>(this.AppServices, this.completedHandler);
        }
    }
}