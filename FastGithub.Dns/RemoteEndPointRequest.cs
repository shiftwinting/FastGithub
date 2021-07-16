using DNS.Protocol;
using System;
using System.Net;
using System.Net.Sockets;

namespace FastGithub.Dns
{
    /// <summary>
    /// 带远程终节点的请求
    /// </summary>
    sealed class RemoteEndPointRequest : Request
    {
        /// <summary>
        /// 获取程终节点
        /// </summary>
        public EndPoint RemoteEndPoint { get; }

        /// <summary>
        /// 远程请求
        /// </summary>
        /// <param name="request"></param>
        /// <param name="remoteEndPoint"></param>
        public RemoteEndPointRequest(Request request, EndPoint remoteEndPoint)
            : base(request)
        {
            this.RemoteEndPoint = remoteEndPoint;
        }

        /// <summary>
        /// 获取对应的本机地址
        /// </summary> 
        /// <returns></returns>
        public IPAddress? GetLocalAddress()
        {
            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.Connect(this.RemoteEndPoint);
                return socket.LocalEndPoint is IPEndPoint localEndPoint ? localEndPoint.Address : default;
            }
            catch (Exception)
            {
                return default;
            }
        }
    }
}
