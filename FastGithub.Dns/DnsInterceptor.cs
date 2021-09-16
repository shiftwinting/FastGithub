using DNS.Protocol;
using DNS.Protocol.ResourceRecords;
using FastGithub.Configuration;
using Microsoft.Extensions.Logging;
using PacketDotNet;
using System;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using WinDivertSharp;
using WinDivertSharp.WinAPI;

namespace FastGithub.Dns
{
    /// <summary>
    /// dns拦截器
    /// </summary>   
    [SupportedOSPlatform("windows")]
    sealed class DnsInterceptor
    {
        private const string DNS_FILTER = "udp.DstPort == 53";
        private const int ERROR_IO_PENDING = 997;
        private readonly FastGithubConfig fastGithubConfig;
        private readonly ILogger<DnsInterceptor> logger;
        private readonly TimeSpan ttl = TimeSpan.FromSeconds(10d);

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

            DnsFlushResolverCache();

            var packetLength = 0U;
            var packetBuffer = new byte[ushort.MaxValue];
            using var winDivertBuffer = new WinDivertBuffer(packetBuffer);
            var winDivertAddress = new WinDivertAddress();

            while (cancellationToken.IsCancellationRequested == false)
            {
                if (this.WinDivertRecvEx(handle, winDivertBuffer, ref winDivertAddress, ref packetLength, cancellationToken))
                {
                    try
                    {
                        this.ProcessDnsPacket(packetBuffer, ref winDivertAddress, ref packetLength);
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogWarning(ex.Message);
                    }

                    WinDivert.WinDivertHelperCalcChecksums(winDivertBuffer, packetLength, ref winDivertAddress, WinDivertChecksumHelperParam.All);
                    WinDivert.WinDivertSend(handle, winDivertBuffer, packetLength, ref winDivertAddress);
                }
            }

            WinDivert.WinDivertClose(handle);
            DnsFlushResolverCache();
        }

        /// <summary>
        /// 接收数据
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="winDivertBuffer"></param>
        /// <param name="winDivertAddress"></param>
        /// <param name="packetLength"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private bool WinDivertRecvEx(IntPtr handle, WinDivertBuffer winDivertBuffer, ref WinDivertAddress winDivertAddress, ref uint packetLength, CancellationToken cancellationToken)
        {
            using var waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset, null);
            var overlapped = new NativeOverlapped
            {
                EventHandle = waitHandle.SafeWaitHandle.DangerousGetHandle()
            };

            winDivertAddress.Reset();
            if (WinDivert.WinDivertRecvEx(handle, winDivertBuffer, 0, ref winDivertAddress, ref packetLength, ref overlapped))
            {
                return true;
            }

            var error = Marshal.GetLastWin32Error();
            if (error != ERROR_IO_PENDING)
            {
                this.logger.LogWarning($"Unknown IO error ID {error} while awaiting overlapped result.");
                return false;
            }

            while (waitHandle.WaitOne(TimeSpan.FromSeconds(1d)) == false)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            var asyncPacketLength = 0U;
            if (Kernel32.GetOverlappedResult(handle, ref overlapped, ref asyncPacketLength, false) == false)
            {
                this.logger.LogWarning("Failed to get overlapped result.");
                return false;
            }

            packetLength = asyncPacketLength;
            return true;
        }

        /// <summary>
        /// 处理DNS数据包
        /// </summary>
        /// <param name="packetBuffer"></param>
        /// <param name="winDivertAddress"></param>
        /// <param name="packetLength"></param>
        private void ProcessDnsPacket(byte[] packetBuffer, ref WinDivertAddress winDivertAddress, ref uint packetLength)
        {
            var packetData = packetBuffer.AsSpan(0, (int)packetLength).ToArray();
            var packet = Packet.ParsePacket(LinkLayers.Raw, packetData);
            var ipPacket = (IPPacket)packet.PayloadPacket;
            var udpPacket = (UdpPacket)ipPacket.PayloadPacket;

            var request = Request.FromArray(udpPacket.PayloadData);
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

            // 反转ip
            var sourceAddress = ipPacket.SourceAddress;
            ipPacket.SourceAddress = ipPacket.DestinationAddress;
            ipPacket.DestinationAddress = sourceAddress;

            // 反转端口
            var sourcePort = udpPacket.SourcePort;
            udpPacket.SourcePort = udpPacket.DestinationPort;
            udpPacket.DestinationPort = sourcePort;

            // 设置dns响应
            var response = Response.FromRequest(request);
            var record = new IPAddressResourceRecord(domain, IPAddress.Loopback, this.ttl);
            response.AnswerRecords.Add(record);
            udpPacket.PayloadData = response.ToArray();

            // 修改数据内容和数据长度
            packet.Bytes.CopyTo(packetBuffer, 0);
            packetLength = (uint)packet.Bytes.Length;

            // 反转方向
            if (winDivertAddress.Direction == WinDivertDirection.Inbound)
            {
                winDivertAddress.Direction = WinDivertDirection.Outbound;
            }
            else
            {
                winDivertAddress.Direction = WinDivertDirection.Inbound;
            }
        }
    }
}
