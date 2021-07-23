using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Http
{
    /// <summary>
    /// 表示http客户端
    /// </summary>
    public class HttpClient : HttpMessageInvoker
    {
        private readonly DomainConfig domainConfig;

        /// <summary>
        /// http客户端
        /// </summary>
        /// <param name="domainConfig"></param>
        /// <param name="domainResolver"></param>
        public HttpClient(DomainConfig domainConfig, IDomainResolver domainResolver)
            : this(domainConfig, new HttpClientHandler(domainResolver), disposeHandler: true)
        {
            this.domainConfig = domainConfig;
        }

        /// <summary>
        /// http客户端
        /// </summary>
        /// <param name="domainConfig"></param>
        /// <param name="handler"></param>
        /// <param name="disposeHandler"></param>
        internal HttpClient(DomainConfig domainConfig, HttpClientHandler handler, bool disposeHandler)
            : base(handler, disposeHandler)
        {
            this.domainConfig = domainConfig;
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
                TlsSniPattern = this.domainConfig.GetTlsSniPattern(),
                TlsIgnoreNameMismatch = this.domainConfig.TlsIgnoreNameMismatch
            });
            return base.SendAsync(request, cancellationToken);
        }
    }
}