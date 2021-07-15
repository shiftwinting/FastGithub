using System.Net;

namespace FastGithub.Scanner
{
    /// <summary>
    /// 定义扫描结果的接口
    /// </summary>
    public interface IGithubScanResults
    {
        /// <summary>
        /// 是否支持指定域名
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        bool Support(string domain);

        /// <summary>
        /// 查找最优的ip
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        IPAddress? FindBestAddress(string domain);
    }
}
