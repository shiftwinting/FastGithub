using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Middlewares
{
    sealed class HttpsScanMiddleware : IGithubScanMiddleware
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
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"https://{context.Address}"),
                };
                request.Headers.Host = context.Domain;

                using var httpClient = new HttpClient(new HttpClientHandler
                {
                    Proxy = null,
                    UseProxy = false,
                });

                var startTime = DateTime.Now;
                using var cancellationTokenSource = new CancellationTokenSource(this.options.CurrentValue.HttpsScanTimeout);
                var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationTokenSource.Token);
                var server = response.EnsureSuccessStatusCode().Headers.Server;
                if (server.Any(s => string.Equals("GitHub.com", s.Product?.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    context.HttpElapsed = DateTime.Now.Subtract(startTime);
                    await next();
                }
            }
            catch (TaskCanceledException)
            {
                this.logger.LogTrace($"{context.Domain} {context.Address}连接超时");
            }
            catch (Exception ex)
            {
                this.logger.LogTrace($"{context.Domain} {context.Address} {ex.Message}");
            }
        }
    }
}
