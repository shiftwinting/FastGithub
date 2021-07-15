using System.Net;

namespace FastGithub.Scanner
{
    /// <summary>
    /// github解析器
    /// </summary>
    public interface IGithubResolver
    {
        /// <summary>
        /// 是否支持指定的域名
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        bool IsSupported(string domain);

        /// <summary>
        /// 解析指定的域名
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        IPAddress? Resolve(string domain);
    }
}
