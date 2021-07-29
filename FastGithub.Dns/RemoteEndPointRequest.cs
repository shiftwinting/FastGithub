using DNS.Protocol;
using FastGithub.Configuration;
using System.Net;

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
            return LocalMachine.GetLocalAddress(this.RemoteEndPoint);
        }
    }
}
