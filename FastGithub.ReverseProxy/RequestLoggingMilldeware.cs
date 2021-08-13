using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

namespace FastGithub.ReverseProxy
{
    /// <summary>
    /// 请求日志中间件
    /// </summary>
    sealed class RequestLoggingMiddleware
    {
        private readonly ILogger<RequestLoggingMiddleware> logger;

        /// <summary>
        /// 请求日志中间件
        /// </summary>
        /// <param name="logger"></param>
        public RequestLoggingMiddleware(ILogger<RequestLoggingMiddleware> logger)
        {
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
            var stopwatch = Stopwatch.StartNew();

            try
            {
                await next(context);
            }
            finally
            {
                stopwatch.Stop();
            }

            var request = context.Request;
            var response = context.Response;
            var message = $"{request.Method} {request.Scheme}://{request.Host}{request.Path} responded {response.StatusCode} in {stopwatch.Elapsed.TotalMilliseconds} ms";

            var client = context.Connection.RemoteIpAddress;
            if (IPAddress.Loopback.Equals(client) == false)
            {
                message = $"{client} {message}";
            }

            var exception = context.GetForwarderErrorFeature()?.Exception;
            if (exception == null)
            {
                this.logger.LogInformation(message);
            }
            else
            {
                this.logger.LogError($"{message}{Environment.NewLine}{exception}");
            }
        }
    }
}
