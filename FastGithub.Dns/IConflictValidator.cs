using System.Threading.Tasks;

namespace FastGithub.Dns
{
    /// <summary>
    /// Dns冲突验证器
    /// </summary>
    interface IConflictValidator
    {
        /// <summary>
        /// 验证冲突
        /// </summary>
        /// <returns></returns>
        Task ValidateAsync();
    }
}