using DNS.Client;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Scanner
{
    /// <summary>
    /// 由本程序提值的dns的httpHandler
    /// </summary>
    [Service(ServiceLifetime.Transient)]
    sealed class LoopbackDnsHttpHandler : DelegatingHandler
    {
        /// <summary>
        /// 本程序的dns
        /// </summary>
        private static readonly DnsClient dnsClient = new(IPAddress.Loopback);

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri;
            if (uri != null && uri.HostNameType == UriHostNameType.Dns)
            {
                var address = await LookupAsync(uri.Host);
                if (address != null)
                {
                    var builder = new UriBuilder(uri)
                    {
                        Host = address.ToString()
                    };
                    request.RequestUri = builder.Uri;
                    request.Headers.Host = uri.Host;
                }
            }

            return await base.SendAsync(request, cancellationToken);
        }

        /// <summary>
        /// dns解析ip
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        private static async Task<IPAddress?> LookupAsync(string host)
        {
            try
            {
                using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(500d));
                var addresses = await dnsClient.Lookup(host, cancellationToken: cancellationTokenSource.Token);
                return addresses.FirstOrDefault();
            }
            catch (Exception)
            {
                return default;
            }
        }
    }
}
