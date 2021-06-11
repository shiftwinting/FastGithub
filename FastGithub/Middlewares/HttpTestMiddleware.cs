using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Middlewares
{
    sealed class HttpTestMiddleware : IGithubMiddleware
    {
        private readonly IOptionsMonitor<GithubOptions> options;
        private readonly ILogger<HttpTestMiddleware> logger;

        public HttpTestMiddleware(
            IOptionsMonitor<GithubOptions> options,
            ILogger<HttpTestMiddleware> logger)
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
                    RequestUri = new Uri($"https://{context.Address}/"),
                };
                request.Headers.Host = context.Domain;

                using var httpClient = new HttpClient(new HttpClientHandler
                {
                    Proxy = null,
                    UseProxy = false,
                    ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                });

                var startTime = DateTime.Now;
                using var cancellationTokenSource = new CancellationTokenSource(this.options.CurrentValue.HttpTestTimeout);
                var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationTokenSource.Token);
                var server = response.EnsureSuccessStatusCode().Headers.Server;
                if (server.Any(s => string.Equals("GitHub.com", s.Product?.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    context.HttpElapsed = DateTime.Now.Subtract(startTime);
                    this.logger.LogWarning(context.ToString());

                    await next();
                }
            }
            catch (Exception ex)
            {
                this.logger.LogInformation($"{context.Domain} {context.Address} {ex.Message}");
            }
        }
    }
}
