using System;
using System.Threading.Tasks;

namespace FastGithub
{
    /// <summary>
    /// 中间件创建者扩展
    /// </summary>
    static class GithubBuilderExtensions
    {
        /// <summary>
        /// 使用中间件
        /// </summary> 
        /// <typeparam name="TMiddleware"></typeparam>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IGithubBuilder Use<TMiddleware>(this IGithubBuilder builder) where TMiddleware : class, IGithubMiddleware
        {
            return builder.AppServices.GetService(typeof(TMiddleware)) is TMiddleware middleware
               ? builder.Use(middleware.InvokeAsync)
               : throw new InvalidOperationException($"无法获取服务{typeof(TMiddleware)}");
        }

        /// <summary>
        /// 使用中间件
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <param name="builder"></param>
        /// <param name="middleware"></param>
        /// <returns></returns>
        public static IGithubBuilder Use(this IGithubBuilder builder, Func<GithubContext, Func<Task>, Task> middleware)
        {
            return builder.Use(next => context => middleware(context, () => next(context)));
        }
    }
}
