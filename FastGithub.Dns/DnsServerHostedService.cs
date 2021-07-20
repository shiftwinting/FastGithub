using DNS.Protocol;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Dns
{
    /// <summary>
    /// dns后台服务
    /// </summary>
    sealed class DnsServerHostedService : BackgroundService
    {
        private readonly RequestResolver requestResolver;
        private readonly HostsValidator hostsValidator;
        private readonly ILogger<DnsServerHostedService> logger;

        private readonly Socket socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private readonly byte[] buffer = new byte[ushort.MaxValue];


        /// <summary>
        /// dns后台服务
        /// </summary>
        /// <param name="requestResolver"></param>
        /// <param name="hostsValidator"></param>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public DnsServerHostedService(
            RequestResolver requestResolver,
            HostsValidator hostsValidator,
            IOptionsMonitor<FastGithubOptions> options,
            ILogger<DnsServerHostedService> logger)
        {
            this.requestResolver = requestResolver;
            this.hostsValidator = hostsValidator;
            this.logger = logger;

            options.OnChange(opt =>
            {
                if (OperatingSystem.IsWindows())
                {
                    SystemDnsUtil.DnsFlushResolverCache();
                }
            });
        }

        /// <summary>
        /// 启动dns
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await this.BindAsync(cancellationToken);
            this.logger.LogInformation("DNS服务启动成功");

            this.SetAsPrimitiveNameServer();
            await this.hostsValidator.ValidateAsync();

            await base.StartAsync(cancellationToken);
        }

        /// <summary>
        /// 尝试多次绑定
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task BindAsync(CancellationToken cancellationToken)
        {
            const int DNS_PORT = 53;
            if (OperatingSystem.IsWindows() && UdpTable.TryGetOwnerProcessId(DNS_PORT, out var processId))
            {
                Process.GetProcessById(processId).Kill();
            }

            var localEndPoint = new IPEndPoint(IPAddress.Any, DNS_PORT);
            var delay = TimeSpan.FromMilliseconds(100d);
            for (var i = 10; i >= 0; i--)
            {
                try
                {
                    this.socket.Bind(localEndPoint);
                    break;
                }
                catch (Exception)
                {
                    if (i == 0)
                    {
                        throw new FastGithubException($"无法监听{localEndPoint}，udp端口已被其它程序占用");
                    }
                    await Task.Delay(delay, cancellationToken);
                }
            }

            if (OperatingSystem.IsWindows())
            {
                const int SIO_UDP_CONNRESET = unchecked((int)0x9800000C);
                this.socket.IOControl(SIO_UDP_CONNRESET, new byte[4], new byte[4]);
            }
        }

        /// <summary>
        /// 设置自身为主dns
        /// </summary>
        private void SetAsPrimitiveNameServer()
        {
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    SystemDnsUtil.DnsSetPrimitive(IPAddress.Loopback);
                    SystemDnsUtil.DnsFlushResolverCache();
                    this.logger.LogInformation($"设置为本机主DNS成功");
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning($"设置为本机主DNS失败：{ex.Message}");
                }
            }
            else
            {
                this.logger.LogWarning("平台不支持自动设置DNS，请手动设置网卡的主DNS为127.0.0.1");
            }
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
                try
                {
                    var result = await this.socket.ReceiveFromAsync(this.buffer, SocketFlags.None, remoteEndPoint);
                    var datas = new byte[result.ReceivedBytes];
                    this.buffer.AsSpan(0, datas.Length).CopyTo(datas);
                    this.HandleRequestAsync(datas, result.RemoteEndPoint, stoppingToken);
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
        /// 停止dns服务
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            this.socket.Dispose();
            this.logger.LogInformation("DNS服务已停止");

            if (OperatingSystem.IsWindows())
            {
                try
                {
                    SystemDnsUtil.DnsFlushResolverCache();
                    SystemDnsUtil.DnsRemovePrimitive(IPAddress.Loopback);
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning($"恢复DNS记录失败：{ex.Message}");
                }
            }

            return base.StopAsync(cancellationToken);
        }
    }
}
