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
        private readonly bool tlsSni;

        /// <summary>
        /// YARP的HttpClient
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="disposeHandler"></param>
        /// <param name="tlsSni"></param>
        public HttpClient(HttpMessageHandler handler, bool disposeHandler, bool tlsSni) :
            base(handler, disposeHandler)
        {
            this.tlsSni = tlsSni;
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
            request.SetSniContext(new SniContext(isHttps, this.tlsSni));
            return base.SendAsync(request, cancellationToken);
        }
    }
}
