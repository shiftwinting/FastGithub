using FastGithub.Configuration;
using FastGithub.DomainResolve;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System.IO.Pipelines;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Forwarder;

namespace FastGithub.ReverseProxy
{
    /// <summary>
    /// http代理中间件
    /// </summary>
    sealed class HttpProxyMiddleware
    {
        private readonly FastGithubConfig fastGithubConfig;
        private readonly IDomainResolver domainResolver;
        private readonly IHttpForwarder httpForwarder;
        private readonly PortService portService;
        private readonly SocketsHttpHandler socketsHttpHandler = new() { UseCookies = false, UseProxy = false, AllowAutoRedirect = false, AutomaticDecompression = DecompressionMethods.None };

        /// <summary>
        /// http代理中间件
        /// </summary>
        /// <param name="fastGithubConfig"></param>
        /// <param name="domainResolver"></param>
        /// <param name="httpForwarder"></param>
        /// <param name="portService"></param>
        public HttpProxyMiddleware(
            FastGithubConfig fastGithubConfig,
            IDomainResolver domainResolver,
            IHttpForwarder httpForwarder,
            PortService portService)
        {
            this.fastGithubConfig = fastGithubConfig;
            this.domainResolver = domainResolver;
            this.httpForwarder = httpForwarder;
            this.portService = portService;
        }

        /// <summary>
        /// 处理请求
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (context.Request.Method != HttpMethods.Connect)
            {
                var httpClient = new HttpMessageInvoker(this.socketsHttpHandler, false);
                var destinationPrefix = $"{context.Request.Scheme}://{context.Request.Host}";
                await this.httpForwarder.SendAsync(context, destinationPrefix, httpClient);
            }
            else
            {
                var endpoint = await this.GetTargetEndPointAsync(context.Request);
                using var targetSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                await targetSocket.ConnectAsync(endpoint);

                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Features.Get<IHttpResponseFeature>().ReasonPhrase = "Connection Established";
                await context.Response.CompleteAsync();

                var transport = context.Features.Get<IConnectionTransportFeature>()?.Transport;
                if (transport != null)
                {
                    var targetStream = new NetworkStream(targetSocket, ownsSocket: false);
                    var task1 = targetStream.CopyToAsync(transport.Output);
                    var task2 = transport.Input.CopyToAsync(targetStream);
                    await Task.WhenAny(task1, task2);
                }
            }
        }


        /// <summary>
        /// 获取目标终节点
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private async Task<EndPoint> GetTargetEndPointAsync(HttpRequest request)
        {
            var domain = request.Host.Host;
            var port = request.Host.Port ?? 443;

            if (IPAddress.TryParse(domain, out var address) == true)
            {
                return new IPEndPoint(address, port);
            }

            if (this.fastGithubConfig.TryGetDomainConfig(domain, out _) == false)
            {
                return new DnsEndPoint(domain, port);
            }

            // https，走反向代理中间人
            if (port == 443)
            {
                return new IPEndPoint(IPAddress.Loopback, this.portService.HttpsReverseProxyPort);
            }

            // dns优选
            address = await this.domainResolver.ResolveAsync(new DnsEndPoint(domain, port));
            return new IPEndPoint(address, port);
        }
    }
}