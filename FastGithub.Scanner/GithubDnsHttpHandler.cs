using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Scanner
{
    /// <summary>
    /// Github的dns解析的httpHandler
    /// 使扫描索结果作为github的https请求的域名解析
    /// </summary>
    [Service(ServiceLifetime.Transient)]
    sealed class GithubDnsHttpHandler : DelegatingHandler
    {
        private readonly GithubScanResults scanResults;

        /// <summary>
        /// Github的dns解析的httpHandler
        /// </summary>
        public GithubDnsHttpHandler(GithubScanResults scanResults)
        {
            this.scanResults = scanResults;
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
