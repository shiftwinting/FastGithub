using DNS.Protocol;
using DNS.Protocol.ResourceRecords;
using FastGithub.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using WindivertDotnet;

namespace FastGithub.PacketIntercept.Dns
{
    /// <summary>
    /// dns拦截器
    /// </summary>   
    [SupportedOSPlatform("windows")]
    sealed class DnsInterceptor : IDnsInterceptor
    {
        private static readonly Filter filter = Filter.True.And(f => f.Udp.DstPort == 53);

        private readonly FastGithubConfig fastGithubConfig;
        private readonly ILogger<DnsInterceptor> logger;

        private readonly TimeSpan ttl = TimeSpan.FromMinutes(5d);

        /// <summary>
        /// 刷新DNS缓存
        /// </summary>    
        [DllImport("dnsapi.dll", EntryPoint = "DnsFlushResolverCache", SetLastError = true)]
        private static extern void DnsFlushResolverCache();

        /// <summary>
        /// 首次加载驱动往往有异常，所以要提前加载
        /// </summary>
        static DnsInterceptor()
        {
            try
            {
                using (new WinDivert(Filter.False, WinDivertLayer.Network)) { }
            }
            catch (Exception) { }
        }

        /// <summary>
        /// dns拦截器
        /// </summary>
        /// <param name="fastGithubConfig"></param>
        /// <param name="logger"></param>
        /// <param name="options"></param>
        public DnsInterceptor(
            FastGithubConfig fastGithubConfig,
            ILogger<DnsInterceptor> logger,
            IOptionsMonitor<FastGithubOptions> options)
        {
            this.fastGithubConfig = fastGithubConfig;
            this.logger = logger;

            options.OnChange(_ => DnsFlushResolverCache());
        }

        /// <summary>
        /// DNS拦截
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <exception cref="Win32Exception"></exception>
        /// <returns></returns>
        public async Task InterceptAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();

            using var divert = new WinDivert(filter, WinDivertLayer.Network);
            cancellationToken.Register(d =>
            {
                ((WinDivert)d!).Dispose();
                DnsFlushResolverCache();
            }, divert);

            var addr = new WinDivertAddress();
            using var packet = new WinDivertPacket();

            DnsFlushResolverCache();
            while (cancellationToken.IsCancellationRequested == false)
            {
                divert.Recv(packet, ref addr);
                try
                {
                    this.ModifyDnsPacket(packet, ref addr);
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
        /// 修改DNS数据包
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="addr"></param>
        unsafe private void ModifyDnsPacket(WinDivertPacket packet, ref WinDivertAddress addr)
        {
            var result = packet.GetParseResult();
            var requestPayload = result.DataSpan.ToArray();

            if (TryParseRequest(requestPayload, out var request) == false ||
                request.OperationCode != OperationCode.Query ||
                request.Questions.Count == 0)
            {
                return;
            }

            var question = request.Questions.First();
            if (question.Type != RecordType.A && question.Type != RecordType.AAAA)
            {
                return;
            }

            var domain = question.Name;
            if (this.fastGithubConfig.IsMatch(question.Name.ToString()) == false)
            {
                return;
            }

            // dns响应数据
            var response = Response.FromRequest(request);
            var loopback = question.Type == RecordType.A ? IPAddress.Loopback : IPAddress.IPv6Loopback;
            var record = new IPAddressResourceRecord(domain, loopback, this.ttl);
            response.AnswerRecords.Add(record);
            var responsePayload = response.ToArray();

            // 修改payload和包长 
            responsePayload.CopyTo(new Span<byte>(result.Data, responsePayload.Length));
            packet.Length = packet.Length + responsePayload.Length - requestPayload.Length;

            // 修改ip包
            IPAddress destAddress;
            if (result.IPV4Header != null)
            {
                destAddress = result.IPV4Header->DstAddr;
                result.IPV4Header->DstAddr = result.IPV4Header->SrcAddr;
                result.IPV4Header->SrcAddr = destAddress;
                result.IPV4Header->Length = (ushort)packet.Length;
            }
            else
            {
                destAddress = result.IPV6Header->DstAddr;
                result.IPV6Header->DstAddr = result.IPV6Header->SrcAddr;
                result.IPV6Header->SrcAddr = destAddress;
                result.IPV6Header->Length = (ushort)(packet.Length - sizeof(IPV6Header));
            }

            // 修改udp包
            var destPort = result.UdpHeader->DstPort;
            result.UdpHeader->DstPort = result.UdpHeader->SrcPort;
            result.UdpHeader->SrcPort = destPort;
            result.UdpHeader->Length = (ushort)(sizeof(UdpHeader) + responsePayload.Length);

            addr.Flags |= WinDivertAddressFlag.Impostor;
            if (addr.Flags.HasFlag(WinDivertAddressFlag.Loopback))
            {
                addr.Flags |= WinDivertAddressFlag.Outbound;
            }
            else
            {
                addr.Flags ^= WinDivertAddressFlag.Outbound;
            } 

            packet.CalcChecksums(ref addr);
            this.logger.LogInformation($"{domain}->{loopback}");
        }


        /// <summary>
        /// 尝试解析请求
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        static bool TryParseRequest(byte[] payload, [MaybeNullWhen(false)] out Request request)
        {
            try
            {
                request = Request.FromArray(payload);
                return true;
            }
            catch (Exception)
            {
                request = null;
                return false;
            }
        }
    }
}
