using DNS.Protocol;
using FastGithub.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Dns
{
    /// <summary>
    /// dns服务器
    /// </summary>
    sealed class DnsServer : IDisposable
    {
        private readonly RequestResolver requestResolver;
        private readonly ILogger<DnsServer> logger;
        private readonly Socket socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private readonly byte[] buffer = new byte[ushort.MaxValue];

        /// <summary>
        /// dns服务器
        /// </summary>
        /// <param name="requestResolver"></param>
        /// <param name="logger"></param>
        public DnsServer(
            RequestResolver requestResolver,
            ILogger<DnsServer> logger)
        {
            this.requestResolver = requestResolver;
            this.logger = logger;
        }

        /// <summary>
        /// 绑定地址和端口
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param> 
        /// <returns></returns>
        public void Bind(IPAddress address, int port)
        {
            if (OperatingSystem.IsWindows())
            {
                UdpTable.KillPortOwner(port);
            }

            var udpListeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners();
            if (udpListeners.Any(item => item.Port == port))
            {
                throw new FastGithubException($"udp端口{port}已经被其它进程占用");
            }

            if (OperatingSystem.IsWindows())
            {
                const int SIO_UDP_CONNRESET = unchecked((int)0x9800000C);
                this.socket.IOControl(SIO_UDP_CONNRESET, new byte[4], new byte[4]);
            }
            this.socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            this.socket.Bind(new IPEndPoint(address, port));
        }

        /// <summary>
        /// 监听dns请求
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task ListenAsync(CancellationToken cancellationToken)
        {
            var remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            while (cancellationToken.IsCancellationRequested == false)
            {
                try
                {
                    var result = await this.socket.ReceiveFromAsync(this.buffer, SocketFlags.None, remoteEndPoint);
                    var datas = new byte[result.ReceivedBytes];
                    this.buffer.AsSpan(0, datas.Length).CopyTo(datas);
                    this.HandleRequestAsync(datas, result.RemoteEndPoint, cancellationToken);
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.OperationAborted)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// 处理dns请求
        /// </summary>
        /// <param name="datas"></param>
        /// <param name="remoteEndPoint"></param>
        /// <param name="cancellationToken"></param>
        private async void HandleRequestAsync(byte[] datas, EndPoint remoteEndPoint, CancellationToken cancellationToken)
        {
            try
            {
                var request = Request.FromArray(datas);
                var remoteEndPointRequest = new RemoteEndPointRequest(request, remoteEndPoint);
                var response = await this.requestResolver.Resolve(remoteEndPointRequest, cancellationToken);
                await this.socket.SendToAsync(response.ToArray(), SocketFlags.None, remoteEndPoint);
            }
            catch (Exception ex)
            {
                this.logger.LogTrace($"处理DNS异常：{ex.Message}");
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            this.socket.Dispose();
        }
    }
}
