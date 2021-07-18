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
        private readonly string tlsSniValue;

        /// <summary>
        /// YARP的HttpClient
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="tlsSniValue"></param>
        /// <param name="disposeHandler"></param>
        public HttpClient(HttpMessageHandler handler, string tlsSniValue, bool disposeHandler = false) :
            base(handler, disposeHandler)
        {
            this.tlsSniValue = tlsSniValue;
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
            request.SetSniContext(new SniContext(isHttps, this.tlsSniValue));
            return base.SendAsync(request, cancellationToken);
        }
    }
}
