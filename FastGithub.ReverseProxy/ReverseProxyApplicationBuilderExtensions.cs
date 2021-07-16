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
            var httpForwarder = app.ApplicationServices.GetRequiredService<IHttpForwarder>();
            var httpClientHanlder = app.ApplicationServices.GetRequiredService<NoSniHttpClientHanlder>();
            var options = app.ApplicationServices.GetRequiredService<IOptionsMonitor<FastGithubOptions>>();

            app.Use(next => async context =>
            {
                var host = context.Request.Host.Host;
                if (options.CurrentValue.IsMatch(host) == false)
                {
                    await context.Response.WriteAsJsonAsync(new { message = $"不支持以{host}访问" });
                    return;
                }

                var port = context.Request.Host.Port ?? 443;
                var destinationPrefix = $"https://{host}:{port}/";
                var httpClient = new HttpMessageInvoker(httpClientHanlder, disposeHandler: false);
                var error = await httpForwarder.SendAsync(context, destinationPrefix, httpClient);

                if (error != ForwarderError.None)
                {
                    var errorFeature = context.GetForwarderErrorFeature();
                    if (errorFeature != null)
                    {
                        await context.Response.WriteAsJsonAsync(new
                        {
                            error = error.ToString(),
                            message = errorFeature.Exception?.Message
                        });
                    }
                }
            });

            return app;
        }
    }
}
