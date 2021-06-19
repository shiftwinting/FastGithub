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
        private readonly IOptionsMonitor<HttpsScanOptions> options;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly ILogger<HttpsScanMiddleware> logger;

        /// <summary>
        /// https扫描中间件
        /// </summary>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public HttpsScanMiddleware(
            IOptionsMonitor<HttpsScanOptions> options,
            IHttpClientFactory httpClientFactory,
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

                var setting = this.options.CurrentValue;
                if (setting.Rules.TryGetValue(context.Domain, out var rule) == false)
                {
                    rule = new HttpsScanOptions.ScanRule();
                }

                using var request = new HttpRequestMessage();
                request.Method = new HttpMethod(rule.Method);
                request.RequestUri = new Uri(new Uri($"https://{context.Address}"), rule.Path);
                request.Headers.Host = context.Domain;
                request.Headers.ConnectionClose = setting.ConnectionClose;

                var timeout = this.options.CurrentValue.Timeout;
                using var timeoutTokenSource = new CancellationTokenSource(timeout);
                using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutTokenSource.Token, context.CancellationToken);

                var httpClient = this.httpClientFactory.CreateClient(nameof(FastGithub));
                using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, linkedTokenSource.Token);

                VerifyHttpsResponse(context.Domain, response);
                context.Available = true;

                await next();
            }
            catch (Exception ex)
            {
                context.CancellationToken.ThrowIfCancellationRequested();
                this.logger.LogTrace($"{context.Domain} {context.Address} { GetInnerMessage(ex)}");
            }
        }

        /// <summary>
        /// 验证响应内容
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="response"></param>
        /// <exception cref="HttpRequestException"></exception>
        /// <exception cref="ValidationException"></exception>
        private static void VerifyHttpsResponse(string domain, HttpResponseMessage response)
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

        private static string GetInnerMessage(Exception ex)
        {
            while (ex.InnerException != null)
            {
                return GetInnerMessage(ex.InnerException);
            }
            return ex.Message;
        }
    }
}
