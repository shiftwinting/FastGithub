using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Forwarder;

namespace FastGithub.ReverseProxy
{
    /// <summary>
    /// 反向代理中间件
    /// </summary>
    sealed class ReverseProxyMiddleware
    {
        private readonly IHttpForwarder httpForwarder;
        private readonly SniHttpClientHanlder sniHttpClientHanlder;
        private readonly NoSniHttpClientHanlder noSniHttpClientHanlder;
        private readonly IOptionsMonitor<FastGithubOptions> options;

        public ReverseProxyMiddleware(
            IHttpForwarder httpForwarder,
            SniHttpClientHanlder sniHttpClientHanlder,
            NoSniHttpClientHanlder noSniHttpClientHanlder,
            IOptionsMonitor<FastGithubOptions> options)
        {
            this.httpForwarder = httpForwarder;
            this.sniHttpClientHanlder = sniHttpClientHanlder;
            this.noSniHttpClientHanlder = noSniHttpClientHanlder;
            this.options = options;
        }

        /// <summary>
        /// 处理请求
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task InvokeAsync(HttpContext context)
        {
            var host = context.Request.Host.Host;
            if (this.options.CurrentValue.Config.TryGetDomainConfig(host, out var domainConfig) == false)
            {
                await context.Response.WriteAsJsonAsync(new
                {
                    error = ForwarderError.NoAvailableDestinations.ToString(),
                    message = $"不支持https反向代理{host}这个域名"
                });
                return;
            }

            var destinationPrefix = GetDestinationPrefix(host, domainConfig.Destination);
            var httpClient = domainConfig.NoSni
               ? new HttpMessageInvoker(this.noSniHttpClientHanlder, disposeHandler: false)
               : new HttpMessageInvoker(this.sniHttpClientHanlder, disposeHandler: false);

            var error = await httpForwarder.SendAsync(context, destinationPrefix, httpClient);
            await ResponseErrorAsync(context, error);
        }

        /// <summary>
        /// 获取目标前缀
        /// </summary>
        /// <param name="host"></param> 
        /// <param name="destination"></param>
        /// <returns></returns>
        private static string GetDestinationPrefix(string host, Uri? destination)
        {
            var defaultValue = $"https://{host}/";
            if (destination == null)
            {
                return defaultValue;
            }

            var baseUri = new Uri(defaultValue);
            return new Uri(baseUri, destination).ToString();
        }

        /// <summary>
        /// 写入错误信息
        /// </summary>
        /// <param name="context"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        private static async Task ResponseErrorAsync(HttpContext context, ForwarderError error)
        {
            if (error == ForwarderError.None)
            {
                return;
            }

            var errorFeature = context.GetForwarderErrorFeature();
            if (errorFeature == null)
            {
                return;
            }

            await context.Response.WriteAsJsonAsync(new
            {
                error = error.ToString(),
                message = errorFeature.Exception?.Message
            });
        }
    }
}
