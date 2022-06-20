using Microsoft.AspNetCore.Http;

namespace FastGithub.HttpServer.TcpMiddlewares
{
    interface IHttpProxyFeature
    {
        HostString ProxyHost { get; }

        ProxyProtocol ProxyProtocol { get; }
    }
}
