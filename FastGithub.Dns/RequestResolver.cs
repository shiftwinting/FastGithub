using DNS.Client.RequestResolver;
using DNS.Protocol;
using DNS.Protocol.ResourceRecords;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Dns
{
    /// <summary>
    /// dns解析者
    /// </summary> 
    sealed class RequestResolver : IRequestResolver
    {
        private IRequestResolver requestResolver;

        private readonly TimeSpan ttl = TimeSpan.FromMinutes(1d);
        private readonly FastGithubConfig fastGithubConfig;
        private readonly ILogger<RequestResolver> logger;

        /// <summary>
        /// dns解析者
        /// </summary>
        /// <param name="fastGithubConfig"></param>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public RequestResolver(
            FastGithubConfig fastGithubConfig,
            IOptionsMonitor<FastGithubOptions> options,
            ILogger<RequestResolver> logger)
        {
            this.fastGithubConfig = fastGithubConfig;
            this.logger = logger;

            this.requestResolver = new UdpRequestResolver(fastGithubConfig.UnTrustedDns);
            options.OnChange(opt => DnsConfigChanged(opt.UntrustedDns));

            void DnsConfigChanged(DnsConfig config)
            {
                var dns = config.ToIPEndPoint();
                this.requestResolver = new UdpRequestResolver(dns);
            }
        }

        /// <summary>
        /// 解析域名
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IResponse> Resolve(IRequest request, CancellationToken cancellationToken = default)
        {
            var response = Response.FromRequest(request);
            if (request is not RemoteEndPointRequest remoteEndPointRequest)
            {
                return response;
            }

            var question = request.Questions.FirstOrDefault();
            if (question == null || question.Type != RecordType.A)
            {
                return response;
            }

            // 解析匹配的域名指向本机ip
            var domain = question.Name;
            if (this.fastGithubConfig.IsMatch(domain.ToString()) == true)
            {
                var localAddress = remoteEndPointRequest.GetLocalAddress() ?? IPAddress.Loopback;
                var record = new IPAddressResourceRecord(domain, localAddress, this.ttl);
                response.AnswerRecords.Add(record);

                this.logger.LogInformation($"[{domain}->{localAddress}]");
                return response;
            }

            return await this.requestResolver.Resolve(request, cancellationToken);
        }
    }
}
