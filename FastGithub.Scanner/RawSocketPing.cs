using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Scanner
{
    /// <summary>
    /// RawSocket的ping功能
    /// </summary>
    static class RawSocketPing
    {
        private static readonly byte[] echoRequestPacket = Convert.FromHexString("0800F6FF0100000000000000");
        private static readonly byte[] icmpReceiveBuffer = new byte[72];

        /// <summary>
        /// 获取是否支持
        /// </summary>
        public static bool IsSupported { get; private set; }

        /// <summary>
        /// RawSocket的ping功能
        /// </summary>
        static RawSocketPing()
        {
            try
            {
                new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp).Dispose();
                IsSupported = true;
            }
            catch (Exception)
            {
                IsSupported = false;
            }
        }

        /// <summary>
        /// ping目标ip
        /// </summary>
        /// <param name="destAddresses"></param>
        /// <param name="timeWait">等待时间</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>ping通的ip</returns>
        public static async Task<HashSet<IPAddress>> PingAsync(IEnumerable<IPAddress> destAddresses, TimeSpan timeWait, CancellationToken cancellationToken = default)
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp)
            {
                Ttl = 128,
                DontFragment = false
            };
            socket.Bind(new IPEndPoint(IPAddress.Any, 0));

            using var cancellationTokenSource = new CancellationTokenSource();
            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancellationTokenSource.Token);
            var receiveTask = ReceiveAsync(socket, linkedTokenSource.Token);

            var distinctDestAddresses = destAddresses.Distinct();
            foreach (var address in distinctDestAddresses)
            {
                var remoteEndPoint = new IPEndPoint(address, 0);
                await socket.SendToAsync(echoRequestPacket, SocketFlags.None, remoteEndPoint);
            }

            await Task.Delay(timeWait, cancellationToken);
            cancellationTokenSource.Cancel();
            socket.Close();

            var hashSet = await receiveTask;
            hashSet.IntersectWith(distinctDestAddresses);
            return hashSet;
        }

        /// <summary>
        /// 循环接收任务
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private static async Task<HashSet<IPAddress>> ReceiveAsync(Socket socket, CancellationToken cancellationToken)
        {
            await Task.Yield();

            var hashSet = new HashSet<IPAddress>();
            var remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

            while (cancellationToken.IsCancellationRequested == false)
            {
                try
                {
                    var result = await socket.ReceiveFromAsync(icmpReceiveBuffer, SocketFlags.None, remoteEndPoint);
                    if (result.RemoteEndPoint is IPEndPoint ipEndPoint)
                    {
                        hashSet.Add(ipEndPoint.Address);
                    }
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.OperationAborted)
                {
                    break;
                }
                catch (Exception)
                {
                }
            }
            return hashSet;
        }
    }
}
