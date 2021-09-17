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

namespace FastGithub.HttpServer
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
                context.Response.ContentType = "application/x-ns-proxy-autoconfig";
                var pacString = this.GetProxyPacString(context);
                await context.Response.WriteAsync(pacString);
            }
            else if (context.Request.Method == HttpMethods.Connect)
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
            else
            {
                var destinationPrefix = $"{context.Request.Scheme}://{context.Request.Host}";
                await this.httpForwarder.SendAsync(context, destinationPrefix, this.httpClient);
            }
        }


        /// <summary>
        /// 获取proxypac脚本
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private string GetProxyPacString(HttpContext context)
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
            return buidler.ToString();
        }

        /// <summary>
        /// 获取目标终节点
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private async Task<EndPoint> GetTargetEndPointAsync(HttpRequest request)
        {
            const int HTTPS_PORT = 443;
            var targetHost = request.Host.Host;
            var targetPort = request.Host.Port ?? HTTPS_PORT;

            if (IPAddress.TryParse(targetHost, out var address) == true)
            {
                return new IPEndPoint(address, targetPort);
            }

            // 不关心的域名，直接使用系统dns
            if (this.fastGithubConfig.IsMatch(targetHost) == false)
            {
                return new DnsEndPoint(targetHost, targetPort);
            }

            // 目标端口为443，走https代理中间人
            if (targetPort == HTTPS_PORT)
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