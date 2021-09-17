using DNS.Protocol;
using DNS.Protocol.ResourceRecords;
using FastGithub.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers.Binary;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using WinDivertSharp;

namespace FastGithub.Dns
{
    /// <summary>
    /// dns拦截器
    /// </summary>   
    [SupportedOSPlatform("windows")]
    sealed class DnsInterceptor
    {
        private const string DNS_FILTER = "udp.DstPort == 53";
        private readonly FastGithubConfig fastGithubConfig;
        private readonly ILogger<DnsInterceptor> logger;
        private readonly TimeSpan ttl = TimeSpan.FromMinutes(2d);

        /// <summary>
        /// 刷新DNS缓存
        /// </summary>    
        [DllImport("dnsapi.dll", EntryPoint = "DnsFlushResolverCache", SetLastError = true)]
        private static extern void DnsFlushResolverCache();

        /// <summary>
        /// dns投毒后台服务
        /// </summary>
        /// <param name="fastGithubConfig"></param>
        /// <param name="logger"></param>
        public DnsInterceptor(
            FastGithubConfig fastGithubConfig,
            ILogger<DnsInterceptor> logger)
        {
            this.fastGithubConfig = fastGithubConfig;
            this.logger = logger;
        }

        /// <summary>
        /// DNS拦截
        /// </summary>
        /// <param name="cancellationToken"></param>
        public void Intercept(CancellationToken cancellationToken)
        {
            var handle = WinDivert.WinDivertOpen(DNS_FILTER, WinDivertLayer.Network, 0, WinDivertOpenFlags.None);
            if (handle == IntPtr.Zero)
            {
                return;
            }

            cancellationToken.Register(hwnd =>
            {
                WinDivert.WinDivertClose((IntPtr)hwnd!);
                DnsFlushResolverCache();
            }, handle);

            var packetLength = 0U;
            using var winDivertBuffer = new WinDivertBuffer();
            var winDivertAddress = new WinDivertAddress();

            DnsFlushResolverCache();
            while (cancellationToken.IsCancellationRequested == false)
            {
                if (WinDivert.WinDivertRecv(handle, winDivertBuffer, ref winDivertAddress, ref packetLength))
                {
                    try
                    {
                        this.ModifyDnsPacket(winDivertBuffer, ref winDivertAddress, ref packetLength);
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
        /// 修改DNS数据包
        /// </summary>
        /// <param name="winDivertBuffer"></param>
        /// <param name="winDivertAddress"></param>
        /// <param name="packetLength"></param> 
        unsafe private void ModifyDnsPacket(WinDivertBuffer winDivertBuffer, ref WinDivertAddress winDivertAddress, ref uint packetLength)
        {
            var packet = WinDivert.WinDivertHelperParsePacket(winDivertBuffer, packetLength);
            var requestPayload = new Span<byte>(packet.PacketPayload, (int)packet.PacketPayloadLength).ToArray();

            var request = Request.FromArray(requestPayload);
            if (request.OperationCode != OperationCode.Query)
            {
                return;
            }

            var question = request.Questions.FirstOrDefault();
            if (question == null || question.Type != RecordType.A)
            {
                return;
            }

            var domain = question.Name;
            if (this.fastGithubConfig.IsMatch(domain.ToString()) == false)
            {
                return;
            }

            // dns响应数据
            var response = Response.FromRequest(request);
            var record = new IPAddressResourceRecord(domain, IPAddress.Loopback, this.ttl);
            response.AnswerRecords.Add(record);
            var responsePayload = response.ToArray();

            // 修改payload和包长 
            responsePayload.CopyTo(new Span<byte>(packet.PacketPayload, responsePayload.Length));
            packetLength += (uint)responsePayload.Length - packet.PacketPayloadLength;

            // 修改ip包
            if (packet.IPv4Header != null)
            {
                var destAddress = packet.IPv4Header->DstAddr;
                packet.IPv4Header->DstAddr = packet.IPv4Header->SrcAddr;
                packet.IPv4Header->SrcAddr = destAddress;
                packet.IPv4Header->Length = BinaryPrimitives.ReverseEndianness((ushort)packetLength);
            }
            else
            {
                var destAddress = packet.IPv6Header->DstAddr;
                packet.IPv6Header->DstAddr = packet.IPv6Header->SrcAddr;
                packet.IPv6Header->SrcAddr = destAddress;
                packet.IPv6Header->Length = BinaryPrimitives.ReverseEndianness((ushort)packetLength);
            }

            // 修改udp包
            var destPort = packet.UdpHeader->DstPort;
            packet.UdpHeader->DstPort = packet.UdpHeader->SrcPort;
            packet.UdpHeader->SrcPort = destPort;
            packet.UdpHeader->Length = BinaryPrimitives.ReverseEndianness((ushort)(responsePayload.Length + 8));

            // 反转方向
            if (winDivertAddress.Direction == WinDivertDirection.Inbound)
            {
                winDivertAddress.Direction = WinDivertDirection.Outbound;
            }
            else
            {
                winDivertAddress.Direction = WinDivertDirection.Inbound;
            }

            WinDivert.WinDivertHelperCalcChecksums(winDivertBuffer, packetLength, ref winDivertAddress, WinDivertChecksumHelperParam.All);
            this.logger.LogInformation($"已拦截dns查询{domain}并伪造响应内容为{IPAddress.Loopback}");
        }
    }
}
