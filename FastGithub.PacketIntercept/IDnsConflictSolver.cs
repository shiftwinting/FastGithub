using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.PacketIntercept
{
    /// <summary>
    /// Dns冲突解决者
    /// </summary>
    interface IDnsConflictSolver
    {
        /// <summary>
        /// 解决冲突
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SolveAsync(CancellationToken cancellationToken);

        /// <summary>
        /// 恢复冲突
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task RestoreAsync(CancellationToken cancellationToken);
    }
}