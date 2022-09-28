using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using WindivertDotnet;

namespace FastGithub.PacketIntercept.Tcp
{
    /// <summary>
    /// tcp拦截器
    /// </summary>   
    [SupportedOSPlatform("windows")]
    abstract class TcpInterceptor : ITcpInterceptor
    {
        private readonly Filter filter;
        private readonly ushort oldServerPort;
        private readonly ushort newServerPort;
        private readonly ILogger logger;

        /// <summary>
        /// tcp拦截器
        /// </summary>
        /// <param name="oldServerPort">修改前的服务器端口</param>
        /// <param name="newServerPort">修改后的服务器端口</param>
        /// <param name="logger"></param>
        public TcpInterceptor(int oldServerPort, int newServerPort, ILogger logger)
        {
            this.filter = Filter.True
                .And(f => f.Network.Loopback)
                .And(f => f.Tcp.DstPort == oldServerPort || f.Tcp.SrcPort == newServerPort);

            this.oldServerPort = (ushort)oldServerPort;
            this.newServerPort = (ushort)newServerPort;
            this.logger = logger;
        }

        /// <summary>
        /// 拦截指定端口的数据包
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <exception cref="Win32Exception"></exception>
        public async Task InterceptAsync(CancellationToken cancellationToken)
        {
            if (this.oldServerPort == this.newServerPort)
            {
                return;
            }

            await Task.Yield();

            using var divert = new WinDivert(this.filter, WinDivertLayer.Network, 0, WinDivertFlag.None);
            if (Socket.OSSupportsIPv4)
            {
                this.logger.LogInformation($"{IPAddress.Loopback}:{this.oldServerPort} <=> {IPAddress.Loopback}:{this.newServerPort}");
            }
            if (Socket.OSSupportsIPv6)
            {
                this.logger.LogInformation($"{IPAddress.IPv6Loopback}:{this.oldServerPort} <=> {IPAddress.IPv6Loopback}:{this.newServerPort}");
            }
            cancellationToken.Register(d => ((WinDivert)d!).Dispose(), divert);

            var addr = new WinDivertAddress();
            using var packet = new WinDivertPacket();
            while (cancellationToken.IsCancellationRequested == false)
            {
                addr.Clear();
                divert.Recv(packet, ref addr);

                try
                {
                    this.ModifyTcpPacket(packet, ref addr);
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning(ex.Message);
                }
                finally
                {
                    divert.Send(packet, ref addr);
                }
            }
        }

        /// <summary>
        /// 修改tcp数据端口的端口
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="addr"></param>
        unsafe private void ModifyTcpPacket(WinDivertPacket packet, ref WinDivertAddress addr)
        {
            var result = packet.GetParseResult();
            if (result.IPV4Header != null && result.IPV4Header->SrcAddr.Equals(IPAddress.Loopback) == false)
            {
                return;
            }
            if (result.IPV6Header != null && result.IPV6Header->SrcAddr.Equals(IPAddress.IPv6Loopback) == false)
            {
                return;
            }

            if (result.TcpHeader->DstPort == oldServerPort)
            {
                result.TcpHeader->DstPort = this.newServerPort;
            }
            else
            {
                result.TcpHeader->SrcPort = oldServerPort;
            }
            addr.Flags |= WinDivertAddressFlag.Impostor;
            packet.CalcChecksums(ref addr);
        }
    }
}
