using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.PacketIntercept
{
    /// <summary>
    /// tcp拦截器接口
    /// </summary>
    interface ITcpInterceptor
    {
        /// <summary>
        /// 拦截数据包
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task InterceptAsync(CancellationToken cancellationToken);
    }
}