using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.PacketIntercept
{
    /// <summary>
    /// dns拦截器接口
    /// </summary>
    interface IDnsInterceptor
    {
        /// <summary>
        /// 拦截数据包
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task InterceptAsync(CancellationToken cancellationToken);
    }
}