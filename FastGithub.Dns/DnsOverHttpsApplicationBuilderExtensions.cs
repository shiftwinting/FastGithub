using FastGithub.Dns;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace FastGithub
{
    /// <summary>
    /// DoH的中间件扩展
    /// </summary>
    public static class DnsOverHttpsApplicationBuilderExtensions
    {
        /// <summary>
        /// 使用DoH的中间件
        /// </summary>
        /// <param name="app"></param> 
        /// <returns></returns>
        public static IApplicationBuilder UseDnsOverHttps(this IApplicationBuilder app)
        {
            var middleware = app.ApplicationServices.GetRequiredService<DnsOverHttpsMiddleware>();
            return app.Use(next => context => middleware.InvokeAsync(context, next));
        }
    }
}
