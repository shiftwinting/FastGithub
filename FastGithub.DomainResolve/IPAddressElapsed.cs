using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.DomainResolve
{
    /// <summary>
    /// IP延时
    /// </summary>
    [DebuggerDisplay("Adddress={Adddress} Elapsed={Elapsed}")]
    struct IPAddressElapsed
    {
        private static readonly TimeSpan maxConnectTimeout = TimeSpan.FromSeconds(5d);

        /// <summary>
        /// 获取IP地址
        /// </summary>
        public IPAddress Adddress { get; }

        /// <summary>
        /// 获取延时
        /// </summary>
        public TimeSpan Elapsed { get; }

        /// <summary>
        /// IP延时
        /// </summary>
        /// <param name="adddress"></param>
        /// <param name="elapsed"></param>
        public IPAddressElapsed(IPAddress adddress, TimeSpan elapsed)
        {
            this.Adddress = adddress;
            this.Elapsed = elapsed;
        }

        /// <summary>
        /// 获取连接耗时
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<IPAddressElapsed> ParseAsync(IPAddress address, int port, CancellationToken cancellationToken)
        {
            var stopWatch = Stopwatch.StartNew();
            try
            {
                using var timeoutTokenSource = new CancellationTokenSource(maxConnectTimeout);
                using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutTokenSource.Token);
                using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                await socket.ConnectAsync(address, port, linkedTokenSource.Token);
                return new IPAddressElapsed(address, stopWatch.Elapsed);
            }
            catch (Exception)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return new IPAddressElapsed(address, TimeSpan.MaxValue);
            }
            finally
            {
                stopWatch.Stop();
            }
        }
    }
}
