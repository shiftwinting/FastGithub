using System;

namespace FastGithub.Scanner
{
    sealed class GithubContextStatistics
    {
        /// <summary>
        /// 扫描总次数
        /// </summary>
        public int TotalScanCount { get; private set; }


        /// <summary>
        /// 扫描总成功次数
        /// </summary>
        public int TotalSuccessCount { get; private set; }

        /// <summary>
        /// 扫描总耗时
        /// </summary>
        public TimeSpan TotalSuccessElapsed { get; private set; }


        public void SetScan()
        {
            this.TotalScanCount += 1;
        }

        public void SetScanSuccess(TimeSpan elapsed)
        {
            this.TotalSuccessCount += 1;
            this.TotalSuccessElapsed = this.TotalSuccessElapsed.Add(elapsed);
        }

        /// <summary>
        /// 获取成功率
        /// </summary>
        /// <returns></returns>
        public double GetSuccessRate()
        {
            return this.TotalScanCount > 0 ?
                (double)this.TotalSuccessCount / this.TotalScanCount
                : 0d;
        }

        /// <summary>
        /// 获取平均耗时
        /// </summary>
        /// <returns></returns>
        public TimeSpan GetAvgElapsed()
        {
            return this.TotalScanCount > 0
                ? this.TotalSuccessElapsed / this.TotalScanCount
                : TimeSpan.MaxValue;
        }
    }
}
