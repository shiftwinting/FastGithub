using FastGithub.DomainResolve;
using Microsoft.AspNetCore.Connections;
using System;
using System.IO;
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
        private const int SSH_PORT = 22;
        private const string GITHUB_COM = "github.com";
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
            var address = await this.domainResolver.ResolveAsync(GITHUB_COM, CancellationToken.None);
            using var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectAsync(new IPEndPoint(address, SSH_PORT));

            var upStream = new NetworkStream(socket, ownsSocket: false);
            var downStream = new DuplexStream(connection.Transport);

            var task1 = upStream.CopyToAsync(downStream);
            var task2 = downStream.CopyToAsync(upStream);
            await Task.WhenAny(task1, task2);
        }

        /// <summary>
        /// 双工数据流
        /// </summary>
        private class DuplexStream : Stream
        {
            private readonly Stream readStream;
            private readonly Stream wirteStream;

            /// <summary>
            /// 双工数据流
            /// </summary>
            /// <param name="transport"></param>
            public DuplexStream(IDuplexPipe transport)
            {
                this.readStream = transport.Input.AsStream();
                this.wirteStream = transport.Output.AsStream();
            }

            public override bool CanRead => true;

            public override bool CanSeek => false;

            public override bool CanWrite => true;

            public override long Length => throw new NotSupportedException();

            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override void Flush()
            {
                this.wirteStream.Flush();
            }

            public override Task FlushAsync(CancellationToken cancellationToken)
            {
                return this.wirteStream.FlushAsync(cancellationToken);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return this.readStream.Read(buffer, offset, count);
            }
            public override void Write(byte[] buffer, int offset, int count)
            {
                this.wirteStream.Write(buffer, offset, count);
            }
            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                return this.readStream.ReadAsync(buffer, cancellationToken);
            }

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                return this.readStream.ReadAsync(buffer, offset, count, cancellationToken);
            }

            public override void Write(ReadOnlySpan<byte> buffer)
            {
                this.wirteStream.Write(buffer);
            }

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                return this.wirteStream.WriteAsync(buffer, offset, count, cancellationToken);
            }

            public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            {
                await this.wirteStream.WriteAsync(buffer, cancellationToken);
            }
        }
    }
}
