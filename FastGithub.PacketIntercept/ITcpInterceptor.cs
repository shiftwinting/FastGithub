using System.Threading;

namespace FastGithub.PacketIntercept
{
    /// <summary>
    /// tcp拦截器接口
    /// </summary>
    interface ITcpInterceptor
    {
        /// <summary>
        /// 拦截tcp
        /// </summary>
        /// <param name="cancellationToken"></param>
        void Intercept(CancellationToken cancellationToken);
    }
}