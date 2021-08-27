using DNS.Protocol;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace FastGithub.Dns
{
    /// <summary>
    /// DoH中间件
    /// </summary>
    sealed class DnsOverHttpsMiddleware
    {
        private static readonly PathString dnsQueryPath = "/dns-query";
        private const string MEDIA_TYPE = "application/dns-message";
        private readonly RequestResolver requestResolver;
        private readonly ILogger<DnsOverHttpsMiddleware> logger;

        /// <summary>
        /// DoH中间件
        /// </summary>
        /// <param name="requestResolver"></param>
        /// <param name="logger"></param>
        public DnsOverHttpsMiddleware(
            RequestResolver requestResolver,
            ILogger<DnsOverHttpsMiddleware> logger)
        {
            this.requestResolver = requestResolver;
            this.logger = logger;
        }

        /// <summary>
        /// 执行请求
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            Request? request;
            try
            {
                request = await ParseDnsRequestAsync(context.Request);
            }
            catch (Exception)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            if (request == null)
            {
                await next(context);
                return;
            }

            var response = await this.ResolveAsync(context, request);
            context.Response.ContentType = MEDIA_TYPE;
            await context.Response.BodyWriter.WriteAsync(response.ToArray());
        }

        /// <summary>
        /// 解析dns域名
        /// </summary>
        /// <param name="context"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        private async Task<IResponse> ResolveAsync(HttpContext context, Request request)
        {
            try
            {
                var remoteIPAddress = context.Connection.RemoteIpAddress ?? IPAddress.Loopback;
                var remoteEndPoint = new IPEndPoint(remoteIPAddress, context.Connection.RemotePort);
                var remoteEndPointRequest = new RemoteEndPointRequest(request, remoteEndPoint);
                return await this.requestResolver.Resolve(remoteEndPointRequest);
            }
            catch (Exception ex)
            {
                this.logger.LogWarning($"处理DNS异常：{ex.Message}");
                return Response.FromRequest(request);
            }
        }

        /// <summary>
        /// 解析dns请求
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private static async Task<Request?> ParseDnsRequestAsync(HttpRequest request)
        {
            if (request.Path != dnsQueryPath ||
                request.Headers.TryGetValue("accept", out var accept) == false ||
                accept.Contains(MEDIA_TYPE) == false)
            {
                return default;
            }

            if (request.Method == HttpMethods.Get)
            {
                if (request.Query.TryGetValue("dns", out var dns) == false)
                {
                    return default;
                }

                var dnsRequest = dns.ToString().Replace('-', '+').Replace('_', '/');
                int mod = dnsRequest.Length % 4;
                if (mod > 0)
                {
                    dnsRequest = dnsRequest.PadRight(dnsRequest.Length - mod + 4, '=');
                }

                var message = Convert.FromBase64String(dnsRequest);
                return Request.FromArray(message);
            }

            if (request.Method == HttpMethods.Post && request.ContentType == MEDIA_TYPE)
            {
                using var message = new MemoryStream();
                await request.Body.CopyToAsync(message);
                return Request.FromArray(message.ToArray());
            }

            return default;
        }
    }
}
