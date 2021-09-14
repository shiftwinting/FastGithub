using FastGithub.Configuration;
using FastGithub.DomainResolve;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System.IO.Pipelines;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
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
        private readonly HttpMessageInvoker httpClient;

        /// <summary>
        /// http代理中间件
        /// </summary>
        /// <param name="fastGithubConfig"></param>
        /// <param name="domainResolver"></param>
        /// <param name="httpForwarder"></param>
        public HttpProxyMiddleware(
            FastGithubConfig fastGithubConfig,
            IDomainResolver domainResolver,
            IHttpForwarder httpForwarder)
        {
            this.fastGithubConfig = fastGithubConfig;
            this.domainResolver = domainResolver;
            this.httpForwarder = httpForwarder;
            this.httpClient = new HttpMessageInvoker(CreateHttpHandler(), disposeHandler: false);
        }

        /// <summary>
        /// 处理请求
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Method == HttpMethods.Get && context.Request.Path == "/proxy.pac")
            {
                var buidler = new StringBuilder();
                buidler.AppendLine("function FindProxyForURL(url, host){");
                buidler.AppendLine($"    var proxy = 'PROXY {context.Request.Host}';");
                foreach (var domain in this.fastGithubConfig.GetDomainPatterns())
                {
                    buidler.AppendLine($"    if (shExpMatch(host, '{domain}')) return proxy;");
                }
                buidler.AppendLine("    return 'DIRECT';");
                buidler.AppendLine("}");
                var pacString = buidler.ToString();

                context.Response.ContentType = "application/x-ns-proxy-autoconfig";
                await context.Response.WriteAsync(pacString);
            }
            else if (context.Request.Method != HttpMethods.Connect)
            {
                var destinationPrefix = $"{context.Request.Scheme}://{context.Request.Host}";
                await this.httpForwarder.SendAsync(context, destinationPrefix, this.httpClient);
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
            var targetHost = request.Host.Host;
            var targetPort = request.Host.Port ?? 443;

            if (IPAddress.TryParse(targetHost, out var address) == true)
            {
                return new IPEndPoint(address, targetPort);
            }

            if (this.fastGithubConfig.TryGetDomainConfig(targetHost, out _) == false)
            {
                return new DnsEndPoint(targetHost, targetPort);
            }

            // https，走反向代理中间人
            if (targetPort == 443)
            {
                return new IPEndPoint(IPAddress.Loopback, HttpsReverseProxyPort.Value);
            }

            // dns优选
            address = await this.domainResolver.ResolveAsync(new DnsEndPoint(targetHost, targetPort));
            return new IPEndPoint(address, targetPort);
        }

        /// <summary>
        /// 创建httpHandler
        /// </summary>
        /// <returns></returns>
        private static SocketsHttpHandler CreateHttpHandler()
        {
            return new()
            {
                Proxy = null,
                UseProxy = false,
                UseCookies = false,
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.None
            };
        }
    }
}