using FastGithub.ReverseProxy;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace FastGithub
{
    /// <summary>
    /// https反向代理的中间件扩展
    /// </summary>
    public static class ReverseProxyApplicationBuilderExtensions
    {
        /// <summary>
        /// 使用https反向代理中间件
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseHttpsReverseProxy(this IApplicationBuilder app)
        {
            var middleware = app.ApplicationServices.GetRequiredService<ReverseProxyMiddleware>();
            return app.Use(next => context => middleware.InvokeAsync(context));
        }
    }
}
