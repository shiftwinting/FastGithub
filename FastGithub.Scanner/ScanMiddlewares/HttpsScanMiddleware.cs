using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Scanner.ScanMiddlewares
{
    [Service(ServiceLifetime.Singleton)]
    sealed class HttpsScanMiddleware : IMiddleware<GithubContext>
    {
        private readonly IOptionsMonitor<GithubOptions> options;
        private readonly ILogger<HttpsScanMiddleware> logger;

        public HttpsScanMiddleware(
            IOptionsMonitor<GithubOptions> options,
            ILogger<HttpsScanMiddleware> logger)
        {
            this.options = options;
            this.logger = logger;
        }

        public async Task InvokeAsync(GithubContext context, Func<Task> next)
        {
            try
            {
                context.Available = false;

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"https://{context.Address}"),
                };
                request.Headers.Host = context.Domain;
                request.Headers.ConnectionClose = true;
                request.Headers.Accept.TryParseAdd("*/*");

                using var httpClient = new HttpMessageInvoker(new SocketsHttpHandler
                {
                    Proxy = null,
                    UseProxy = false,
                    AllowAutoRedirect = false,
                });

                var timeout = this.options.CurrentValue.Scan.HttpsScanTimeout;
                using var cancellationTokenSource = new CancellationTokenSource(timeout);
                using var response = await httpClient.SendAsync(request, cancellationTokenSource.Token);
                this.VerifyHttpResponse(context.Domain, response);
                context.Available = true;

                await next();
            }
            catch (TaskCanceledException)
            {
                this.logger.LogTrace($"{context.Domain} {context.Address}连接超时");
            }
            catch (Exception ex)
            {
                var message = GetInnerMessage(ex);
                this.logger.LogTrace($"{context.Domain} {context.Address} {message}");
            }
        }

        /// <summary>
        /// 验证响应内容
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="response"></param>
        /// <exception cref="HttpRequestException"></exception>
        /// <exception cref="ValidationException"></exception>
        private void VerifyHttpResponse(string domain, HttpResponseMessage response)
        {
            response.EnsureSuccessStatusCode();
            if (domain.EndsWith(".github.com"))
            {
                var server = response.Headers.Server;
                if (server.Any(s => string.Equals("github.com", s.Product?.Name, StringComparison.OrdinalIgnoreCase)) == false)
                {
                    throw new ValidationException("伪造的github服务");
                }
            }
        }

        private string GetInnerMessage(Exception ex)
        {
            while (ex.InnerException != null)
            {
                return GetInnerMessage(ex.InnerException);
            }
            return ex.Message;
        }
    }
}
