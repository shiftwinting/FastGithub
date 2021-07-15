using DNS.Client.RequestResolver;
using DNS.Protocol;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Dns
{
    /// <summary>
    /// dns后台服务
    /// </summary>
    sealed class DnsHostedService : BackgroundService
    {
        private const int SIO_UDP_CONNRESET = unchecked((int)0x9800000C);

        private readonly IRequestResolver requestResolver;
        private readonly IOptions<DnsOptions> options;
        private readonly ILogger<DnsHostedService> logger;

        private readonly Socket socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private readonly byte[] buffer = new byte[ushort.MaxValue];
        private IPAddress[]? dnsAddresses;


        /// <summary>
        /// dns后台服务
        /// </summary>
        /// <param name="githubRequestResolver"></param>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public DnsHostedService(
            GithubRequestResolver githubRequestResolver,
            IOptions<DnsOptions> options,
            ILogger<DnsHostedService> logger)
        {
            this.options = options;
            this.logger = logger;

            var upStream = IPAddress.Parse(options.Value.UpStream);
            this.requestResolver = new CompositeRequestResolver(upStream, githubRequestResolver);
        }

        /// <summary>
        /// 启动dns
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            this.socket.Bind(new IPEndPoint(IPAddress.Any, 53));
            if (OperatingSystem.IsWindows())
            {
                this.socket.IOControl(SIO_UDP_CONNRESET, new byte[4], new byte[4]);
            }

            this.logger.LogInformation("dns服务启动成功");
            var upStream = IPAddress.Parse(options.Value.UpStream);
            this.dnsAddresses = this.SetNameServers(IPAddress.Loopback, upStream);
            return base.StartAsync(cancellationToken);
        }

        /// <summary>
        /// dns后台
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            while (stoppingToken.IsCancellationRequested == false)
            {
                var result = await this.socket.ReceiveFromAsync(this.buffer, SocketFlags.None, remoteEndPoint);
                var datas = new byte[result.ReceivedBytes];
                this.buffer.AsSpan(0, datas.Length).CopyTo(datas);
                this.HandleRequestAsync(datas, (IPEndPoint)result.RemoteEndPoint, stoppingToken);
            }
        }

        /// <summary>
        /// 处理dns请求
        /// </summary>
        /// <param name="datas"></param>
        /// <param name="remoteEndPoint"></param>
        /// <param name="cancellationToken"></param>
        private async void HandleRequestAsync(byte[] datas, IPEndPoint remoteEndPoint, CancellationToken cancellationToken)
        {
            try
            {
                var request = Request.FromArray(datas);
                var remoteRequest = new RemoteRequest(request, remoteEndPoint.Address);
                var response = await this.requestResolver.Resolve(remoteRequest, cancellationToken);
                await this.socket.SendToAsync(response.ToArray(), SocketFlags.None, remoteEndPoint);
            }
            catch (Exception ex)
            {
                this.logger.LogTrace($"处理dns异常：{ex.Message}");
            }
        }

        /// <summary>
        /// 停止dns服务
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            this.socket.Dispose();
            this.logger.LogInformation("dns服务已终止");

            if (this.dnsAddresses != null)
            {
                this.SetNameServers(this.dnsAddresses);
            }
            return base.StopAsync(cancellationToken);
        }

        /// <summary>
        /// 设置dns
        /// </summary>
        /// <param name="nameServers"></param>
        /// <returns></returns>
        private IPAddress[]? SetNameServers(params IPAddress[] nameServers)
        {
            if (this.options.Value.SetToLocalMachine && OperatingSystem.IsWindows())
            {
                try
                {
                    var results = NameServiceUtil.SetNameServers(nameServers);
                    this.logger.LogInformation($"设置本机dns成功");
                    return results;
                }

                catch (Exception ex)
                {
                    this.logger.LogWarning($"设置本机dns失败：{ex.Message}");
                }
            }

            return default;
        }

        private class CompositeRequestResolver : IRequestResolver
        {
            private readonly IRequestResolver upStreamResolver;
            private readonly IRequestResolver[] customResolvers;

            public CompositeRequestResolver(IPAddress upStream, params IRequestResolver[] customResolvers)
            {
                this.upStreamResolver = new UdpRequestResolver(new IPEndPoint(upStream, 53));
                this.customResolvers = customResolvers;
            }

            public async Task<IResponse> Resolve(IRequest request, CancellationToken cancellationToken = default)
            {
                foreach (IRequestResolver resolver in customResolvers)
                {
                    var response = await resolver.Resolve(request, cancellationToken);
                    if (response.AnswerRecords.Count > 0)
                    {
                        return response;
                    }
                }

                return await this.upStreamResolver.Resolve(request, cancellationToken);
            }
        }
    }
}
