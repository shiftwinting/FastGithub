using FastGithub.Scanner;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.ReverseProxy
{
    /// <summary>
    /// Github的dns解析的httpHandler
    /// 使扫描索结果作为github的https请求的域名解析
    /// </summary>  
    sealed class GithubDnsHttpHandler : DelegatingHandler
    {
        private readonly IGithubScanResults scanResults;
        private readonly ILogger logger;

        /// <summary>
        /// Github的dns解析的httpHandler
        /// </summary>
        /// <param name="scanResults"></param>
        /// <param name="innerHandler"></param>
        public GithubDnsHttpHandler(
            IGithubScanResults scanResults,
            HttpMessageHandler innerHandler,
            ILogger logger)
            : base(innerHandler)
        {
            this.scanResults = scanResults;
            this.logger = logger;
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri;
            if (uri != null && uri.HostNameType == UriHostNameType.Dns)
            {
                var address = this.scanResults.FindBestAddress(uri.Host);
                if (address != null)
                {
                    this.logger.LogInformation($"使用{address}请求{uri.Host}");
                    var builder = new UriBuilder(uri)
                    {
                        Host = address.ToString()
                    };
                    request.RequestUri = builder.Uri;
                    request.Headers.Host = uri.Host;
                }
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
