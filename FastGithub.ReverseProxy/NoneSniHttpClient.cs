using FastGithub.Scanner;
using System.IO;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.ReverseProxy
{
    /// <summary>
    /// 去掉Sni的HttpClient
    /// </summary> 
    sealed class NoneSniHttpClient : HttpMessageInvoker
    {
        /// <summary>
        /// 去掉Sni的HttpClient
        /// </summary>
        /// <param name="githubScanResults"></param>
        public NoneSniHttpClient(IGithubScanResults githubScanResults)
            : base(CreateNoneSniHttpHandler(githubScanResults), disposeHandler: false)
        {
        }

        /// <summary>
        /// 去掉Sni的HttpHandler
        /// </summary>  
        private static HttpMessageHandler CreateNoneSniHttpHandler(IGithubScanResults githubScanResults)
        {
            var httpHandler = new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                UseCookies = false,
                UseProxy = false,
                ConnectCallback = ConnectCallback
            };

            return new GithubDnsHttpHandler(githubScanResults, httpHandler);
        }

        /// <summary>
        /// 连接回调
        /// </summary>
        /// <param name="context"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private static async ValueTask<Stream> ConnectCallback(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
        {
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectAsync(context.DnsEndPoint, cancellationToken);
            var stream = new NetworkStream(socket, ownsSocket: true);
            if (context.InitialRequestMessage.Headers.Host == null)
            {
                return stream;
            }

            var sslStream = new SslStream(stream, leaveInnerStreamOpen: false, delegate { return true; });
            await sslStream.AuthenticateAsClientAsync(string.Empty, null, false);
            return sslStream;
        }
    }
}
