using FastGithub.DomainResolve;
using Microsoft.AspNetCore.Connections;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace FastGithub.HttpServer
{
    /// <summary>
    /// tcp反射代理处理者
    /// </summary>
    abstract class TcpReverseProxyHandler : ConnectionHandler
    {
        private readonly IDomainResolver domainResolver;
        private readonly DnsEndPoint endPoint;

        /// <summary>
        /// tcp反射代理处理者
        /// </summary>
        /// <param name="domainResolver"></param>
        /// <param name="endPoint"></param>
        public TcpReverseProxyHandler(IDomainResolver domainResolver, DnsEndPoint endPoint)
        {
            this.domainResolver = domainResolver;
            this.endPoint = endPoint;
        }

        /// <summary>
        /// ssh连接后
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task OnConnectedAsync(ConnectionContext context)
        {
            using var targetStream = await this.CreateConnectionAsync();
            var task1 = targetStream.CopyToAsync(context.Transport.Output);
            var task2 = context.Transport.Input.CopyToAsync(targetStream);
            await Task.WhenAny(task1, task2);
        }

        /// <summary>
        /// 创建连接
        /// </summary>
        /// <returns></returns>
        /// <exception cref="AggregateException"></exception>
        private async Task<Stream> CreateConnectionAsync()
        {
            var innerExceptions = new List<Exception>();
            await foreach (var address in this.domainResolver.ResolveAllAsync(this.endPoint))
            {
                var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    await socket.ConnectAsync(address, this.endPoint.Port);
                    return new NetworkStream(socket, ownsSocket: false);
                }
                catch (Exception ex)
                {
                    socket.Dispose();
                    innerExceptions.Add(ex);
                }
            }
            throw new AggregateException($"无法连接到{this.endPoint.Host}:{this.endPoint.Port}", innerExceptions);
        }
    }
}
