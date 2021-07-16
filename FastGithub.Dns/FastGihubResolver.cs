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
    /// 反向代理解析器
    /// </summary> 
    sealed class FastGihubResolver : IRequestResolver
    {
        private readonly IRequestResolver untrustedDnsResolver;
        private readonly IOptionsMonitor<FastGithubOptions> options;
        private readonly ILogger<FastGihubResolver> logger;

        /// <summary>
        /// github相关域名解析器
        /// </summary> 
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public FastGihubResolver(
            IOptionsMonitor<FastGithubOptions> options,
            ILogger<FastGihubResolver> logger)
        {
            this.options = options;
            this.logger = logger;
            this.untrustedDnsResolver = new UdpRequestResolver(options.CurrentValue.UntrustedDns.ToIPEndPoint());
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
            if (request is not RemoteRequest remoteRequest)
            {
                return response;
            }

            var question = request.Questions.FirstOrDefault();
            if (question == null || question.Type != RecordType.A)
            {
                return response;
            }

            var domain = question.Name;
            if (this.options.CurrentValue.IsMatch(domain.ToString()) == true)
            {
                var localAddress = remoteRequest.GetLocalAddress() ?? IPAddress.Loopback;
                var record = new IPAddressResourceRecord(domain, localAddress, TimeSpan.FromMinutes(1d));
                this.logger.LogInformation($"[{domain}->{localAddress}]");
                response.AnswerRecords.Add(record);
                return response;
            }

            return await this.untrustedDnsResolver.Resolve(request, cancellationToken);
        }
    }
}
