using FastGithub.Configuration;
using FastGithub.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Forwarder;

namespace FastGithub.HttpServer
{
    /// <summary>
    /// 反向代理中间件
    /// </summary>
    sealed class HttpReverseProxyMiddleware
    {
        private const string LOOPBACK = "127.0.0.1";
        private const string LOCALHOST = "localhost";
        private const int HTTP_PORT = 80;
        private const int HTTPS_PORT = 443;

        private static readonly DomainConfig defaultDomainConfig = new() { TlsSni = true };

        private readonly IHttpForwarder httpForwarder;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly FastGithubConfig fastGithubConfig;
        private readonly ILogger<HttpReverseProxyMiddleware> logger;


        public HttpReverseProxyMiddleware(
            IHttpForwarder httpForwarder,
            IHttpClientFactory httpClientFactory,
            FastGithubConfig fastGithubConfig,
            ILogger<HttpReverseProxyMiddleware> logger)
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
        /// <param name="next"?
        /// <returns></returns>
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var host = context.Request.Host;
            if (this.TryGetDomainConfig(host, out var domainConfig) == false)
            {
                await next(context);
            }
            else if (domainConfig.Response == null)
            {
                var scheme = context.Request.Scheme;
                var destinationPrefix = GetDestinationPrefix(scheme, host, domainConfig.Destination);
                var httpClient = this.httpClientFactory.CreateHttpClient(host.Host, domainConfig);
                var error = await httpForwarder.SendAsync(context, destinationPrefix, httpClient);
                await HandleErrorAsync(context, error);
            }
            else
            {
                context.Response.StatusCode = domainConfig.Response.StatusCode;
                context.Response.ContentType = domainConfig.Response.ContentType;
                if (domainConfig.Response.ContentValue != null)
                {
                    await context.Response.WriteAsync(domainConfig.Response.ContentValue);
                }
            }
        }

        /// <summary>
        /// 获取域名的DomainConfig
        /// </summary>
        /// <param name="host"></param>
        /// <param name="domainConfig"></param>
        /// <returns></returns>
        private bool TryGetDomainConfig(HostString host, [MaybeNullWhen(false)] out DomainConfig domainConfig)
        {
            if (this.fastGithubConfig.TryGetDomainConfig(host.Host, out domainConfig) == true)
            {
                return true;
            }

            // http(s)://127.0.0.1
            // http(s)://localhost
            if (host.Host == LOOPBACK || host.Host == LOCALHOST)
            {
                if (host.Port == null || host.Port == HTTPS_PORT || host.Port == HTTP_PORT)
                {
                    return false;
                }
            }

            // 未配置的域名，但dns污染解析为127.0.0.1的域名
            this.logger.LogWarning($"检测到{host.Host}可能遭遇了dns污染");
            domainConfig = defaultDomainConfig;
            return true;
        }

        /// <summary>
        /// 获取目标前缀
        /// </summary>
        /// <param name="scheme"></param>
        /// <param name="host"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        private string GetDestinationPrefix(string scheme, HostString host, Uri? destination)
        {
            var defaultValue = $"{scheme}://{host}/";
            if (destination == null)
            {
                return defaultValue;
            }

            var baseUri = new Uri(defaultValue);
            var result = new Uri(baseUri, destination).ToString();
            this.logger.LogInformation($"{defaultValue} => {result}");
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
            if (error == ForwarderError.None || context.Response.HasStarted)
            {
                return;
            }

            await context.Response.WriteAsJsonAsync(new
            {
                error = error.ToString(),
                message = context.GetForwarderErrorFeature()?.Exception?.Message
            });
        }
    }
}
