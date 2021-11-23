using FastGithub.DomainResolve;

namespace FastGithub.HttpServer
{
    /// <summary>
    /// github的ssh代理处理者
    /// </summary>
    sealed class SshReverseProxyHandler : TcpReverseProxyHandler
    {
        /// <summary>
        /// github的ssh代理处理者
        /// </summary>
        /// <param name="domainResolver"></param>
        public SshReverseProxyHandler(IDomainResolver domainResolver)
            : base(domainResolver, new("github.com", 22))
        {
        }
    }
}
