using FastGithub.Configuration;
using FastGithub.DomainResolve;
using System.Net.Http;

namespace FastGithub.Http
{
    /// <summary>
    /// 表示http客户端
    /// </summary>
    public class HttpClient : HttpMessageInvoker
    {
        /// <summary>
        /// http客户端
        /// </summary>
        /// <param name="domainConfig"></param>
        /// <param name="domainResolver"></param>
        public HttpClient(DomainConfig domainConfig, IDomainResolver domainResolver)
            : this(new HttpClientHandler(domainConfig, domainResolver), disposeHandler: true)
        {
        }

        /// <summary>
        /// http客户端
        /// </summary> 
        /// <param name="handler"></param>
        /// <param name="disposeHandler"></param>
        internal HttpClient(HttpClientHandler handler, bool disposeHandler)
            : base(handler, disposeHandler)
        {
        }
    }
}