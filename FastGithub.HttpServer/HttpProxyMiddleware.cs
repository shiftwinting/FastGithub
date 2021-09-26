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
        private const string LOOPBACK = "127.0.0.1";
        private const string LOCALHOST = "localhost";

        private readonly FastGithubConfig fastGithubConfig;
        private readonly IDomainResolver domainResolver;
        private readonly IHttpForwarder httpForwarder;
        private readonly HttpReverseProxyMiddleware httpReverseProxy;

        private readonly HttpMessageInvoker defaultHttpClient;

        /// <summary>
        /// http代理中间件
        /// </summary>
        /// <param name="fastGithubConfig"></param>
        /// <param name="domainResolver"></param>
        /// <param name="httpForwarder"></param>
        /// <param name="httpReverseProxy"></param>
        public HttpProxyMiddleware(
            FastGithubConfig fastGithubConfig,
            IDomainResolver domainResolver,
            IHttpForwarder httpForwarder,
            HttpReverseProxyMiddleware httpReverseProxy)
        {
            this.fastGithubConfig = fastGithubConfig;
            this.domainResolver = domainResolver;
            this.httpForwarder = httpForwarder;
            this.httpReverseProxy = httpReverseProxy;

            this.defaultHttpClient = new HttpMessageInvoker(CreateDefaultHttpHandler(), disposeHandler: false);
        }

        /// <summary>
        /// 处理请求
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task InvokeAsync(HttpContext context)
        {
            var host = context.Request.Host;
            if (this.IsFastGithubServer(host) == true)
            {
                var proxyPac = this.CreateProxyPac(host);
                context.Response.ContentType = "application/x-ns-proxy-autoconfig";
                context.Response.Headers.Add("Content-Disposition", $"attachment;filename=proxy.pac");
                await context.Response.WriteAsync(proxyPac);
            }
            else if (context.Request.Method == HttpMethods.Connect)
            {
                var endpoint = await this.GetTargetEndPointAsync(host);
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
                await this.httpReverseProxy.InvokeAsync(context, async ctx =>
                {
                    var destinationPrefix = $"{ctx.Request.Scheme}://{ctx.Request.Host}";
                    await this.httpForwarder.SendAsync(ctx, destinationPrefix, this.defaultHttpClient);
                });
            }
        }

        /// <summary>
        /// 是否为fastgithub服务
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        private bool IsFastGithubServer(HostString host)
        {
            if (host.Host == LOOPBACK || host.Host == LOCALHOST)
            {
                return host.Port == this.fastGithubConfig.HttpProxyPort;
            }
            return false;
        }

        /// <summary>
        /// 创建proxypac脚本
        /// </summary>
        /// <param name="proxyHost"></param>
        /// <returns></returns>
        private string CreateProxyPac(HostString proxyHost)
        {
            var buidler = new StringBuilder();
            buidler.AppendLine("function FindProxyForURL(url, host){");
            buidler.AppendLine($"    var fastgithub = 'PROXY {proxyHost}';");
            foreach (var domain in this.fastGithubConfig.GetDomainPatterns())
            {
                buidler.AppendLine($"    if (shExpMatch(host, '{domain}')) return fastgithub;");
            }
            buidler.AppendLine("    return 'DIRECT';");
            buidler.AppendLine("}");
            return buidler.ToString();
        }

        /// <summary>
        /// 获取目标终节点
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        private async Task<EndPoint> GetTargetEndPointAsync(HostString host)
        {
            const int HTTP_PORT = 80;
            const int HTTPS_PORT = 443;
            var targetHost = host.Host;
            var targetPort = host.Port ?? HTTPS_PORT;

            if (IPAddress.TryParse(targetHost, out var address) == true)
            {
                return new IPEndPoint(address, targetPort);
            }

            // 不关心的域名，直接使用系统dns
            if (this.fastGithubConfig.IsMatch(targetHost) == false)
            {
                return new DnsEndPoint(targetHost, targetPort);
            }

            if (targetPort == HTTP_PORT)
            {
                return new IPEndPoint(IPAddress.Loopback, ReverseProxyPort.Http);
            }

            if (targetPort == HTTPS_PORT)
            {
                return new IPEndPoint(IPAddress.Loopback, ReverseProxyPort.Https);
            }

            // 不使用系统dns
            address = await this.domainResolver.ResolveAsync(targetHost);
            return new IPEndPoint(address, targetPort);
        }

        /// <summary>
        /// 创建httpHandler
        /// </summary>
        /// <returns></returns>
        private static SocketsHttpHandler CreateDefaultHttpHandler()
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