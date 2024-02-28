using FastGithub.Configuration;
using FastGithub.DomainResolve;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.HttpServer.TcpMiddlewares
{
    /// <summary>
    /// 隧道中间件
    /// </summary>
    sealed class TunnelMiddleware
    {
        private readonly FastGithubConfig fastGithubConfig;
        private readonly IDomainResolver domainResolver;
        private readonly TimeSpan connectTimeout = TimeSpan.FromSeconds(10d);

        /// <summary>
        /// 隧道中间件
        /// </summary>
        /// <param name="fastGithubConfig"></param>
        /// <param name="domainResolver"></param> 
        public TunnelMiddleware(
            FastGithubConfig fastGithubConfig,
            IDomainResolver domainResolver)
        {
            this.fastGithubConfig = fastGithubConfig;
            this.domainResolver = domainResolver;
        }

        /// <summary>
        /// 执行中间件
        /// </summary>
        /// <param name="next"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task InvokeAsync(ConnectionDelegate next, ConnectionContext context)
        {
            var proxyFeature = context.Features.Get<IHttpProxyFeature>();
            if (proxyFeature == null || // 非代理
                proxyFeature.ProxyProtocol != ProxyProtocol.TunnelProxy || //非隧道代理
                context.Features.Get<ITlsConnectionFeature>() != null) // 经过隧道的https
            {
                await next(context);
            }
            else
            {
                var transport = context.Features.Get<IConnectionTransportFeature>()?.Transport;
                if (transport != null)
                {
                    var cancellationToken = context.ConnectionClosed;
                    using var connection = await this.CreateConnectionAsync(proxyFeature.ProxyHost, cancellationToken);

                    var task1 = connection.CopyToAsync(transport.Output, cancellationToken);
                    var task2 = transport.Input.CopyToAsync(connection, cancellationToken);
                    await Task.WhenAny(task1, task2);
                }
            }
        }


        /// <summary>
        /// 创建连接
        /// </summary>
        /// <param name="host"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="AggregateException"></exception>
        private async Task<Stream> CreateConnectionAsync(HostString host, CancellationToken cancellationToken)
        {
            var innerExceptions = new List<Exception>();
            await foreach (var endPoint in this.GetUpstreamEndPointsAsync(host, cancellationToken))
            {
                var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    using var timeoutTokenSource = new CancellationTokenSource(this.connectTimeout);
                    using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutTokenSource.Token);
                    await socket.ConnectAsync(endPoint, linkedTokenSource.Token);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch (Exception ex)
                {
                    socket.Dispose();
                    cancellationToken.ThrowIfCancellationRequested();
                    innerExceptions.Add(ex);
                }
            }
            throw new AggregateException($"无法连接到{host}", innerExceptions);
        }

        /// <summary>
        /// 获取目标终节点
        /// </summary>
        /// <param name="host"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async IAsyncEnumerable<EndPoint> GetUpstreamEndPointsAsync(HostString host, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            const int HTTPS_PORT = 443;
            var targetHost = host.Host;
            var targetPort = host.Port ?? HTTPS_PORT;

            if (IPAddress.TryParse(targetHost, out var address) == true)
            {
                yield return new IPEndPoint(address, targetPort);
            }
            else if (this.fastGithubConfig.IsMatch(targetHost) == false)
            {
                yield return new DnsEndPoint(targetHost, targetPort);
            }
            else
            {
                var dnsEndPoint = new DnsEndPoint(targetHost, targetPort);
                await foreach (var item in this.domainResolver.ResolveAsync(dnsEndPoint, cancellationToken))
                {
                    yield return new IPEndPoint(item, targetPort);
                }
            }
        }
    }
}
