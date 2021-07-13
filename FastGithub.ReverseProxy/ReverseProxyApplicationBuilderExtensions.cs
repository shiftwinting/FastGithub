using FastGithub.ReverseProxy;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Forwarder;

namespace FastGithub
{
    /// <summary>
    /// gitub反向代理的中间件扩展
    /// </summary>
    public static class ReverseProxyApplicationBuilderExtensions
    {
        /// <summary>
        /// 使用gitub反向代理中间件
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseGithubReverseProxy(this IApplicationBuilder app)
        {
            var httpForwarder = app.ApplicationServices.GetRequiredService<IHttpForwarder>();
            var httpClient = app.ApplicationServices.GetRequiredService<NoneSniHttpClient>();

            app.Use(next => async context =>
            {
                var hostString = context.Request.Host;
                var port = hostString.Port ?? 443;
                var destinationPrefix = $"http://{hostString.Host}:{port}/";
                await httpForwarder.SendAsync(context, destinationPrefix, httpClient);
            });

            return app;
        }
    }
}
