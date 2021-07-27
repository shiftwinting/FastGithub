using System.Threading.Tasks;

namespace FastGithub.Dns
{
    /// <summary>
    /// Dns验证器
    /// </summary>
    interface IDnsValidator
    {
        /// <summary>
        /// 验证
        /// </summary>
        /// <returns></returns>
        Task ValidateAsync();
    }
}