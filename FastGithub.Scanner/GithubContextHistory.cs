using System;
using System.Collections.Generic;

namespace FastGithub.Scanner
{
    sealed class GithubContextHistory
    {
        private record ScanLog(DateTime ScanTime, TimeSpan Elapsed);

        private readonly Queue<ScanLog> successLogs = new();

        private readonly Queue<ScanLog> failureLogs = new();

        private static readonly TimeSpan keepLogsTimeSpan = TimeSpan.FromHours(2d);


        public void AddSuccess(TimeSpan elapsed)
        {
            ClearStaleData(this.successLogs, keepLogsTimeSpan);
            this.successLogs.Enqueue(new ScanLog(DateTime.Now, elapsed));
        }

        public void AddFailure()
        {
            ClearStaleData(this.failureLogs, keepLogsTimeSpan);
            this.failureLogs.Enqueue(new ScanLog(DateTime.Now, TimeSpan.Zero));
        }

        static void ClearStaleData(Queue<ScanLog> logs, TimeSpan timeSpan)
        {
            var time = DateTime.Now.Subtract(timeSpan);
            while (logs.TryPeek(out var log))
            {
                if (log.ScanTime < time)
                {
                    logs.TryDequeue(out _);
                }
                break;
            }
        }

        /// <summary>
        /// 获取成功率
        /// </summary>
        /// <returns></returns>
        public double GetSuccessRate()
        {
            var successCount = this.successLogs.Count;
            var totalScanCount = successCount + this.failureLogs.Count;
            return totalScanCount == 0 ? 0d : (double)successCount / totalScanCount;
        }

        /// <summary>
        /// 获取平均耗时
        /// </summary>
        /// <returns></returns>
        public TimeSpan GetAvgElapsed()
        {
            var totalScanCount = this.successLogs.Count + this.failureLogs.Count;
            if (totalScanCount == 0)
            {
                return TimeSpan.MaxValue;
            }

            var totalSuccessElapsed = TimeSpan.Zero;
            foreach (var item in this.successLogs)
            {
                totalSuccessElapsed = totalSuccessElapsed.Add(item.Elapsed);
            }

            return totalSuccessElapsed / totalScanCount;
        }
    }
}
