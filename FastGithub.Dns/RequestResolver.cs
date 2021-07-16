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
        private readonly TimeSpan ttl = TimeSpan.FromMinutes(1d);
        private readonly IRequestResolver untrustedResolver;
        private readonly IOptionsMonitor<FastGithubOptions> options;
        private readonly ILogger<RequestResolver> logger;

        /// <summary>
        /// dns解析者
        /// </summary> 
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public RequestResolver(
            IOptionsMonitor<FastGithubOptions> options,
            ILogger<RequestResolver> logger)
        {
            this.options = options;
            this.logger = logger;
            this.untrustedResolver = new UdpRequestResolver(options.CurrentValue.UntrustedDns.ToIPEndPoint());
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

            var domain = question.Name;
            if (this.options.CurrentValue.IsMatch(domain.ToString()) == true)
            {
                var localAddress = remoteEndPointRequest.GetLocalAddress() ?? IPAddress.Loopback;
                var record = new IPAddressResourceRecord(domain, localAddress, this.ttl);
                response.AnswerRecords.Add(record);

                this.logger.LogInformation($"[{domain}->{localAddress}]");
                return response;
            }

            return await this.untrustedResolver.Resolve(request, cancellationToken);
        }
    }
}
