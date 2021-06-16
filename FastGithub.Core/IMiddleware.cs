using System;
using System.Threading.Tasks;

namespace FastGithub
{
    /// <summary>
    /// 定义中间件的接口
    /// </summary>
    /// <typeparam name="TContext"></typeparam>
    public interface IMiddleware<TContext>
    {
        /// <summary>
        /// 执行中间件
        /// </summary>
        /// <param name="context">上下文</param>
        /// <param name="next">下一个中间件</param>
        /// <returns></returns>
        Task InvokeAsync(TContext context, Func<Task> next);
    }
}
