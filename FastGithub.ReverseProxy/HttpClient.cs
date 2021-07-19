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

        /// <summary>
        /// YARP的HttpClient
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="tlsSniPattern"></param>
        /// <param name="disposeHandler"></param>
        public HttpClient(HttpMessageHandler handler, TlsSniPattern tlsSniPattern, bool disposeHandler = false) :
            base(handler, disposeHandler)
        {
            this.tlsSniPattern = tlsSniPattern;
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var isHttps = request.RequestUri?.Scheme == Uri.UriSchemeHttps;
            request.SetTlsSniContext(new TlsSniContext(isHttps, this.tlsSniPattern));
            return base.SendAsync(request, cancellationToken);
        }
    }
}
