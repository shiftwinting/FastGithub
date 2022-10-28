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
            using var divert = new WinDivert(filter, WinDivertLayer.Network);
            using var packet = new WinDivertPacket();
            using var addr = new WinDivertAddress();

            DnsFlushResolverCache();
            cancellationToken.Register(DnsFlushResolverCache);

            while (cancellationToken.IsCancellationRequested == false)
            {
                await divert.RecvAsync(packet, addr, cancellationToken);
                try
                {
                    this.ModifyDnsPacket(packet, addr);
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning(ex.Message);
                }
                finally
                {
                    await divert.SendAsync(packet, addr, cancellationToken);
                }
            }
        }

        /// <summary>
        /// 修改DNS数据包
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="addr"></param>
        unsafe private void ModifyDnsPacket(WinDivertPacket packet, WinDivertAddress addr)
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

            // 修改payload
            var writer = packet.GetWriter(packet.Length - result.DataLength);
            writer.Write(response.ToArray());

            packet.ReverseEndPoint();
            packet.ApplyLengthToHeaders();
            packet.CalcChecksums(addr);
            packet.CalcOutboundFlag(addr);

            addr.Flags |= WinDivertAddressFlag.Impostor;
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
