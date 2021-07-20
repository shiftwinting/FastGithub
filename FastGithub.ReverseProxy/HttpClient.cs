using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.ReverseProxy
{
    /// <summary>
    /// YARP的HttpClient
    /// </summary>
    class HttpClient : HttpMessageInvoker
    {
        private readonly TlsSniPattern tlsSniPattern;
        private readonly bool tlsIgnoreNameMismatch;

        /// <summary>
        /// YARP的HttpClient
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="tlsSniPattern"></param>
        /// <param name="disposeHandler"></param>
        public HttpClient(
            HttpMessageHandler handler,
            TlsSniPattern tlsSniPattern,
            bool tlsIgnoreNameMismatch,
            bool disposeHandler = false) : base(handler, disposeHandler)
        {
            this.tlsSniPattern = tlsSniPattern;
            this.tlsIgnoreNameMismatch = tlsIgnoreNameMismatch;
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.SetRequestContext(new RequestContext
            {
                Host = request.RequestUri?.Host,
                IsHttps = request.RequestUri?.Scheme == Uri.UriSchemeHttps,
                TlsSniPattern = this.tlsSniPattern,
                TlsIgnoreNameMismatch = this.tlsIgnoreNameMismatch
            });
            return base.SendAsync(request, cancellationToken);
        }
    }
}
