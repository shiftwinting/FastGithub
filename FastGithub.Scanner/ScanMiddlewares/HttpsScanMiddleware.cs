using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Scanner.ScanMiddlewares
{
    /// <summary>
    /// https扫描中间件
    /// </summary>
    [Service(ServiceLifetime.Singleton)]
    sealed class HttpsScanMiddleware : IMiddleware<GithubContext>
    {
        private readonly IOptionsMonitor<GithubOptions> options;
        private readonly HttpClientFactory httpClientFactory;
        private readonly ILogger<HttpsScanMiddleware> logger;

        /// <summary>
        /// https扫描中间件
        /// </summary>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public HttpsScanMiddleware(
            IOptionsMonitor<GithubOptions> options,
            HttpClientFactory httpClientFactory,
            ILogger<HttpsScanMiddleware> logger)
        {
            this.options = options;
            this.httpClientFactory = httpClientFactory;
            this.logger = logger;
        }

        /// <summary>
        /// https扫描
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
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

                var timeout = this.options.CurrentValue.Scan.HttpsScanTimeout;
                using var cancellationTokenSource = new CancellationTokenSource(timeout);
                using var httpClient = this.httpClientFactory.Create(allowAutoRedirect: false);
                using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationTokenSource.Token);

                this.VerifyHttpsResponse(context.Domain, response);
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
        private void VerifyHttpsResponse(string domain, HttpResponseMessage response)
        {
            response.EnsureSuccessStatusCode();

            if (domain == "github.com" || domain.EndsWith(".github.com"))
            {
                if (response.Headers.Server.Any(item => IsGithubServer(item)) == false)
                {
                    throw new ValidationException("伪造的github服务");
                }
            }

            static bool IsGithubServer(ProductInfoHeaderValue headerValue)
            {
                var value = headerValue.Product?.Name;
                return string.Equals("github.com", value, StringComparison.OrdinalIgnoreCase);
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
