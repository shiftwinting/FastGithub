using System;

namespace FastGithub.Scanner
{
    /// <summary>
    /// 扫描选项
    /// </summary>
    [Options("Github:Scan")]
    sealed class GithubScanOptions
    {
        /// <summary>
        /// 完整扫描轮询时间间隔
        /// </summary>

        public TimeSpan FullScanInterval = TimeSpan.FromHours(2d);

        /// <summary>
        /// 结果扫描轮询时间间隔
        /// </summary>
        public TimeSpan ResultScanInterval = TimeSpan.FromMinutes(1d);
    }
}
