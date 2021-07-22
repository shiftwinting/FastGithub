using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Upgrade
{
    /// <summary>
    /// 本机反向代理的httpHandler
    /// </summary>
    sealed class ReverseProxyHttpHandler : DelegatingHandler
    {
        /// <summary>
        /// 本机反向代理的httpHandler
        /// </summary>
        public ReverseProxyHttpHandler()
        {
            this.InnerHandler = new HttpClientHandler();
        }

        /// <summary>
        /// 替换为Loopback
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri;
            if (uri != null && uri.HostNameType == UriHostNameType.Dns)
            {
                var domain = uri.Host;
                var builder = new UriBuilder(uri) { Host = IPAddress.Loopback.ToString() };

                request.RequestUri = builder.Uri;
                request.Headers.Host = domain;
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
