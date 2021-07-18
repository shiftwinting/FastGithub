using DNS.Protocol;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
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
        private readonly FastGithubConfig fastGithubConfig;
        private readonly ILogger<DnsServerHostedService> logger;

        private readonly Socket socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private readonly byte[] buffer = new byte[ushort.MaxValue];
        private IPAddress[]? dnsAddresses;

        [SupportedOSPlatform("windows")]
        [DllImport("dnsapi.dll", EntryPoint = "DnsFlushResolverCache", SetLastError = true)]
        private static extern void DnsFlushResolverCache();

        /// <summary>
        /// dns后台服务
        /// </summary>
        /// <param name="requestResolver"></param>
        /// <param name="fastGithubConfig"></param>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public DnsServerHostedService(
            RequestResolver requestResolver,
            FastGithubConfig fastGithubConfig,
            IOptionsMonitor<FastGithubOptions> options,
            ILogger<DnsServerHostedService> logger)
        {
            this.requestResolver = requestResolver;
            this.fastGithubConfig = fastGithubConfig;
            this.logger = logger;
            options.OnChange(opt => FlushResolverCache());
        }

        /// <summary>
        /// 刷新dns缓存
        /// </summary>
        private static void FlushResolverCache()
        {
            if (OperatingSystem.IsWindows())
            {
                DnsFlushResolverCache();
            }
        }

        /// <summary>
        /// 启动dns
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            const int DNS_PORT = 53;
            if (OperatingSystem.IsWindows() && UdpTable.TryGetOwnerProcessId(DNS_PORT, out var processId))
            {
                Process.GetProcessById(processId).Kill();
            }

            await BindAsync(this.socket, new IPEndPoint(IPAddress.Any, DNS_PORT), cancellationToken);

            if (OperatingSystem.IsWindows())
            {
                const int SIO_UDP_CONNRESET = unchecked((int)0x9800000C);
                this.socket.IOControl(SIO_UDP_CONNRESET, new byte[4], new byte[4]);
            }

            this.logger.LogInformation("dns服务启动成功");
            var secondary = this.fastGithubConfig.FastDns.Address;
            this.dnsAddresses = this.SetNameServers(IPAddress.Loopback, secondary);
            FlushResolverCache();

            await base.StartAsync(cancellationToken);
        }

        /// <summary>
        /// 尝试多次绑定
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="localEndPoint"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private static async Task BindAsync(Socket socket, IPEndPoint localEndPoint, CancellationToken cancellationToken)
        {
            var delay = TimeSpan.FromMilliseconds(100d);
            for (var i = 10; i >= 0; i--)
            {
                try
                {
                    socket.Bind(localEndPoint);
                    break;
                }
                catch (Exception)
                {
                    if (i == 0)
                    {
                        throw new FastGithubException($"无法监听{localEndPoint}，{localEndPoint.Port}的udp端口已被其它程序占用");
                    }
                    await Task.Delay(delay, cancellationToken);
                }
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
            FlushResolverCache();
            return base.StopAsync(cancellationToken);
        }

        /// <summary>
        /// 设置dns
        /// </summary>
        /// <param name="nameServers"></param>
        /// <returns></returns>
        private IPAddress[]? SetNameServers(params IPAddress[] nameServers)
        {
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    var results = SystemDnsUtil.SetNameServers(nameServers);
                    this.logger.LogInformation($"设置本机dns成功");
                    return results;
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning($"设置本机dns失败：{ex.Message}");
                }
            }
            else
            {
                this.logger.LogWarning("不支持自动设置dns，请手动设置网卡的dns为127.0.0.1");
            }

            return default;
        }
    }
}
