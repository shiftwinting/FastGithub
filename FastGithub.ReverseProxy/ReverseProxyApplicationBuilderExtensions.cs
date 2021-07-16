using FastGithub.ReverseProxy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net.Http;
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
            var httpClientHanlder = app.ApplicationServices.GetRequiredService<GithubHttpClientHanlder>();
            var options = app.ApplicationServices.GetRequiredService<IOptionsMonitor<FastGithubOptions>>();

            app.Use(next => async context =>
            {
                var host = context.Request.Host.Host;
                if (options.CurrentValue.IsMatch(host) == false)
                {
                    await context.Response.WriteAsJsonAsync(new { message = $"不支持以{host}访问" });
                }
                else
                {
                    var port = context.Request.Host.Port ?? 443;
                    var destinationPrefix = $"https://{host}:{port}/";
                    var httpClient = new HttpMessageInvoker(httpClientHanlder, disposeHandler: false);
                    await httpForwarder.SendAsync(context, destinationPrefix, httpClient);
                }
            });

            return app;
        }
    }
}
