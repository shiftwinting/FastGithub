using FastGithub.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers.Binary;
using System.Runtime.Versioning;
using System.Threading;
using WinDivertSharp;

namespace FastGithub.Dns
{
    /// <summary>
    /// http拦截器
    /// </summary>   
    [SupportedOSPlatform("windows")]
    sealed class HttpInterceptor
    {
        private readonly ILogger<HttpInterceptor> logger;
        private readonly ushort http80Port = BinaryPrimitives.ReverseEndianness((ushort)80);
        private readonly ushort httpReverseProxyPort = BinaryPrimitives.ReverseEndianness((ushort)ReverseProxyPort.Http);

        /// <summary>
        /// http拦截器
        /// </summary>
        /// <param name="logger"></param>
        public HttpInterceptor(ILogger<HttpInterceptor> logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// 拦截80端口的数据包
        /// </summary>
        /// <param name="cancellationToken"></param>
        public void Intercept(CancellationToken cancellationToken)
        {
            if (ReverseProxyPort.Http == 80)
            {
                return;
            }

            var filter = $"loopback and (tcp.DstPort == 80 or tcp.SrcPort == {ReverseProxyPort.Http})";
            var handle = WinDivert.WinDivertOpen(filter, WinDivertLayer.Network, 0, WinDivertOpenFlags.None);
            if (handle == IntPtr.Zero)
            {
                return;
            }

            cancellationToken.Register(hwnd => WinDivert.WinDivertClose((IntPtr)hwnd!), handle);

            var packetLength = 0U;
            using var winDivertBuffer = new WinDivertBuffer();
            var winDivertAddress = new WinDivertAddress();

            while (cancellationToken.IsCancellationRequested == false)
            {
                if (WinDivert.WinDivertRecv(handle, winDivertBuffer, ref winDivertAddress, ref packetLength))
                {
                    try
                    {
                        this.ModifyHttpsPacket(winDivertBuffer, ref winDivertAddress, ref packetLength);
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogWarning(ex.Message);
                    }
                    finally
                    {
                        WinDivert.WinDivertSend(handle, winDivertBuffer, packetLength, ref winDivertAddress);
                    }
                }
            }
        }

        /// <summary>
        /// 443端口转发到https反向代理端口
        /// </summary>
        /// <param name="winDivertBuffer"></param>
        /// <param name="winDivertAddress"></param>
        /// <param name="packetLength"></param> 
        unsafe private void ModifyHttpsPacket(WinDivertBuffer winDivertBuffer, ref WinDivertAddress winDivertAddress, ref uint packetLength)
        {
            var packet = WinDivert.WinDivertHelperParsePacket(winDivertBuffer, packetLength);
            if (packet.TcpHeader->DstPort == http80Port)
            {
                packet.TcpHeader->DstPort = this.httpReverseProxyPort;
            }
            else
            {
                packet.TcpHeader->SrcPort = http80Port;
            }
            winDivertAddress.Impostor = true;
            WinDivert.WinDivertHelperCalcChecksums(winDivertBuffer, packetLength, ref winDivertAddress, WinDivertChecksumHelperParam.All);
        }
    }
}
