using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.DomainResolve
{
    /// <summary>
    /// IP延时记录
    /// 5分钟有效期
    /// 5秒连接超时
    /// </summary>
    [DebuggerDisplay("Adddress={Adddress} Elapsed={Elapsed}")]
    sealed class IPAddressElapsed : IEquatable<IPAddressElapsed>
    {
        private static readonly long maxLifeTime = 5 * 60 * 1000;
        private static readonly TimeSpan connectTimeout = TimeSpan.FromSeconds(5d);

        private long lastTestTickCount = 0L;

        /// <summary>
        /// 获取IP地址
        /// </summary>
        public IPAddress Adddress { get; }

        /// <summary>
        /// 获取端口
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// 获取延时
        /// </summary>
        public TimeSpan Elapsed { get; private set; } = TimeSpan.MaxValue;

        /// <summary>
        /// IP延时
        /// </summary>
        /// <param name="adddress"></param>
        /// <param name="port"></param>
        public IPAddressElapsed(IPAddress adddress, int port)
        {
            this.Adddress = adddress;
            this.Port = port;
        }

        /// <summary>
        /// 是否可以更新延时
        /// </summary>
        /// <returns></returns>
        public bool CanUpdateElapsed()
        {
            return Environment.TickCount64 - this.lastTestTickCount > maxLifeTime;
        }

        /// <summary>
        /// 更新连接耗时
        /// </summary> 
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task UpdateElapsedAsync(CancellationToken cancellationToken)
        {
            var stopWatch = Stopwatch.StartNew();
            try
            {
                using var timeoutTokenSource = new CancellationTokenSource(connectTimeout);
                using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutTokenSource.Token);
                using var socket = new Socket(this.Adddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                await socket.ConnectAsync(this.Adddress, this.Port, linkedTokenSource.Token);
                this.Elapsed = stopWatch.Elapsed;
            }
            catch (Exception)
            {
                cancellationToken.ThrowIfCancellationRequested();
                this.Elapsed = TimeSpan.MaxValue;
            }
            finally
            {
                this.lastTestTickCount = Environment.TickCount64;
                stopWatch.Stop();
            }
        }

        public bool Equals(IPAddressElapsed? other)
        {
            return other != null && other.Adddress.Equals(this.Adddress);
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is IPAddressElapsed other && this.Equals(other);
        }

        public override int GetHashCode()
        {
            return this.Adddress.GetHashCode();
        }

        public override string ToString()
        {
            return this.Adddress.ToString();
        }
    }
}
