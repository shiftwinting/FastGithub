using FastGithub.DomainResolve;
using Microsoft.AspNetCore.Connections;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.ReverseProxy
{
    /// <summary>
    /// github的ssh处理者
    /// </summary>
    sealed class GithubSshHandler : ConnectionHandler
    {
        private const int SSH_OVER_HTTPS_PORT = 443;
        private const string SSH_GITHUB_COM = "ssh.github.com";
        private readonly IDomainResolver domainResolver;

        /// <summary>
        /// github的ssh处理者
        /// </summary>
        /// <param name="domainResolver"></param>
        public GithubSshHandler(IDomainResolver domainResolver)
        {
            this.domainResolver = domainResolver;
        }

        /// <summary>
        /// ssh连接后
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public override async Task OnConnectedAsync(ConnectionContext connection)
        {
            var address = await this.domainResolver.ResolveAsync(SSH_GITHUB_COM, CancellationToken.None);
            using var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectAsync(new IPEndPoint(address, SSH_OVER_HTTPS_PORT));
            var targetStream = new NetworkStream(socket, ownsSocket: false);

            var task1 = targetStream.CopyToAsync(connection.Transport.Output);
            var task2 = connection.Transport.Input.CopyToAsync(targetStream);
            await Task.WhenAny(task1, task2);
        }
    }
}
