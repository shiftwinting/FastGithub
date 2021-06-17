using System.Net;

namespace FastGithub.Scanner
{
    /// <summary>
    /// 定义扫描结果的接口
    /// </summary>
    public interface IGithubScanResults
    {
        /// <summary>
        /// 查询ip是否可用
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        bool IsAvailable(string domain, IPAddress address);

        /// <summary>
        /// 查找最优的ip
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        IPAddress? FindBestAddress(string domain);
    }
}
