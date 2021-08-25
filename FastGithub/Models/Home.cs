using System.Reflection;

namespace FastGithub.Models
{
    /// <summary>
    /// 首页模型
    /// </summary>
    public class Home
    {
        /// <summary>
        /// 获取版本号
        /// </summary>
        public string? Version { get; } = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        /// <summary>
        /// 请求域名或ip
        /// </summary>
        public string? Host { get; set; }
    }
}
