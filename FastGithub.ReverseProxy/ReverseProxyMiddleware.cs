using FastGithub.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
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
        private readonly IHttpClientFactory httpClientFactory;
        private readonly FastGithubConfig fastGithubConfig;
        private readonly ILogger<ReverseProxyMiddleware> logger;

        public ReverseProxyMiddleware(
            IHttpForwarder httpForwarder,
            IHttpClientFactory httpClientFactory,
            FastGithubConfig fastGithubConfig,
            ILogger<ReverseProxyMiddleware> logger)
        {
            this.httpForwarder = httpForwarder;
            this.httpClientFactory = httpClientFactory;
            this.fastGithubConfig = fastGithubConfig;
            this.logger = logger;
        }

        /// <summary>
        /// 处理请求
        /// </summary>
        /// <param name="context"></param>
        /// <param name="fallbackFile"></param>
        /// <returns></returns>
        public async Task InvokeAsync(HttpContext context, string fallbackFile)
        {
            var host = context.Request.Host.Host;
            if (this.fastGithubConfig.TryGetDomainConfig(host, out var domainConfig) == false)
            {
                context.Response.ContentType = "text/html";
                await context.Response.SendFileAsync(fallbackFile);
            }
            else if (domainConfig.Response != null)
            {
                context.Response.StatusCode = domainConfig.Response.StatusCode;
                context.Response.ContentType = domainConfig.Response.ContentType;
                if (domainConfig.Response.ContentValue != null)
                {
                    await context.Response.WriteAsync(domainConfig.Response.ContentValue);
                }
            }
            else
            {
                var destinationPrefix = GetDestinationPrefix(host, domainConfig.Destination);
                var httpClient = this.httpClientFactory.CreateHttpClient(domainConfig);
                var error = await httpForwarder.SendAsync(context, destinationPrefix, httpClient);
                await HandleErrorAsync(context, error);
            }
        }

        /// <summary>
        /// 获取目标前缀
        /// </summary>
        /// <param name="host"></param> 
        /// <param name="destination"></param>
        /// <returns></returns>
        private string GetDestinationPrefix(string host, Uri? destination)
        {
            var defaultValue = $"https://{host}/";
            if (destination == null)
            {
                return defaultValue;
            }

            var baseUri = new Uri(defaultValue);
            var result = new Uri(baseUri, destination).ToString();
            this.logger.LogInformation($"[{defaultValue} <-> {result}]");
            return result;
        }

        /// <summary>
        /// 处理错误信息
        /// </summary>
        /// <param name="context"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        private static async Task HandleErrorAsync(HttpContext context, ForwarderError error)
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
