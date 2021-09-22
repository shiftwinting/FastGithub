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
    /// https拦截器
    /// </summary>   
    [SupportedOSPlatform("windows")]
    sealed class HttpsInterceptor
    {
        private readonly ILogger<DnsInterceptor> logger;
        private readonly ushort https443Port = BinaryPrimitives.ReverseEndianness((ushort)443);
        private readonly ushort httpReverseProxyPort = BinaryPrimitives.ReverseEndianness((ushort)HttpsReverseProxyPort.Value);

        /// <summary>
        /// https拦截器
        /// </summary>
        /// <param name="logger"></param>
        public HttpsInterceptor(ILogger<DnsInterceptor> logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// 拦截443端口的数据包
        /// </summary>
        /// <param name="cancellationToken"></param>
        public void Intercept(CancellationToken cancellationToken)
        {
            if (HttpsReverseProxyPort.Value == 443)
            {
                return;
            }

            var filter = $"loopback and (tcp.DstPort == 443 or tcp.SrcPort == {HttpsReverseProxyPort.Value})";
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
            if (packet.TcpHeader->DstPort == https443Port)
            {
                packet.TcpHeader->DstPort = this.httpReverseProxyPort;
            }
            else
            {
                packet.TcpHeader->SrcPort = https443Port;
            }
            winDivertAddress.Impostor = true;
            WinDivert.WinDivertHelperCalcChecksums(winDivertBuffer, packetLength, ref winDivertAddress, WinDivertChecksumHelperParam.All);
        }
    }
}
