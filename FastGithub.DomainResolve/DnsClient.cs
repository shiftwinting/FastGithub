using DNS.Client;
using DNS.Client.RequestResolver;
using DNS.Protocol;
using DNS.Protocol.ResourceRecords;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.DomainResolve
{
    /// <summary>
    /// DNS客户端
    /// </summary>
    sealed class DnsClient
    {
        private readonly IPEndPoint dns;
        private readonly IRequestResolver resolver;
        private readonly TimeSpan timeout = TimeSpan.FromSeconds(5d);

        /// <summary>
        /// DNS客户端
        /// </summary>
        /// <param name="dns"></param>
        public DnsClient(IPEndPoint dns)
        {
            this.dns = dns;
            this.resolver = dns.Port == 53
                ? new TcpRequestResolver(dns)
                : new UdpRequestResolver(dns, new TcpRequestResolver(dns));
        }

        /// <summary>
        /// 解析域名
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IPAddress[]> LookupAsync(string domain, CancellationToken cancellationToken = default)
        {
            var request = new Request
            {
                RecursionDesired = true,
                OperationCode = OperationCode.Query
            };
            request.Questions.Add(new Question(new Domain(domain), RecordType.A));
            var clientRequest = new ClientRequest(this.resolver, request);

            using var timeoutTokenSource = new CancellationTokenSource(this.timeout);
            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutTokenSource.Token);
            var response = await clientRequest.Resolve(linkedTokenSource.Token);
            return response.AnswerRecords.OfType<IPAddressResourceRecord>().Select(item => item.IPAddress).ToArray();
        }

        /// <summary>
        /// 转换为文本
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.dns.ToString();
        }
    }
}
