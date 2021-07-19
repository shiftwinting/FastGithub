using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FastGithub.ReverseProxy
{
    /// <summary>
    /// 请求日志中间件
    /// </summary>
    sealed class RequestLoggingMilldeware
    {
        private readonly ILogger<RequestLoggingMilldeware> logger;

        /// <summary>
        /// 请求日志中间件
        /// </summary>
        /// <param name="logger"></param>
        public RequestLoggingMilldeware(ILogger<RequestLoggingMilldeware> logger)
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
            var stopwatch = new Stopwatch();
            stopwatch.Start();

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

            var exception = context.GetForwarderErrorFeature()?.Exception;
            if (exception == null)
            {
                this.logger.LogInformation(message);
            }
            else
            {
                this.logger.LogError($"{message}{Environment.NewLine}{exception.Message}");
            }
        }
    }
}
