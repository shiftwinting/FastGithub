using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;

namespace FastGithub.Scanner
{
    /// <summary>
    /// Github扫描上下文
    /// </summary>
    sealed class GithubContext : DomainAddress, IEquatable<GithubContext>
    {
        /// <summary>
        /// 最多保存最的近的10条记录
        /// </summary>
        private const int MAX_LOG_COUNT = 10;

        /// <summary>
        /// 扫描记录
        /// </summary>
        private record ScanLog(bool Available, TimeSpan Elapsed);

        /// <summary>
        /// 扫描历史记录
        /// </summary>
        private readonly Queue<ScanLog> history = new();



        /// <summary>
        /// 设置取消令牌
        /// </summary>
        public CancellationToken CancellationToken { get; }

        /// <summary>
        /// 获取可用率
        /// </summary>
        /// <returns></returns>
        public double AvailableRate => this.GetAvailableRate();

        /// <summary>
        /// 获取平均耗时
        /// </summary>
        /// <returns></returns>
        public TimeSpan AvgElapsed => this.GetAvgElapsed();

        /// <summary>
        /// 获取或设置是否可用
        /// </summary>
        public bool Available { get; set; }


        /// <summary>
        /// Github扫描上下文
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="address"></param>
        public GithubContext(string domain, IPAddress address)
            : this(domain, address, CancellationToken.None)
        {
        }

        /// <summary>
        /// Github扫描上下文
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="address"></param>
        /// <param name="cancellationToken"></param>
        public GithubContext(string domain, IPAddress address, CancellationToken cancellationToken)
            : base(domain, address)
        {
            this.CancellationToken = cancellationToken;
        }

        /// <summary>
        /// 获取可用率
        /// </summary>
        /// <returns></returns>
        private double GetAvailableRate()
        {
            if (this.history.Count == 0)
            {
                return 0d;
            }

            var availableCount = this.history.Count(item => item.Available);
            return (double)availableCount / this.history.Count;
        }

        /// <summary>
        /// 获取平均耗时
        /// </summary>
        /// <returns></returns>
        private TimeSpan GetAvgElapsed()
        {
            var availableCount = 0;
            var availableElapsed = TimeSpan.Zero;

            foreach (var item in this.history)
            {
                if (item.Available == true)
                {
                    availableCount += 1;
                    availableElapsed = availableElapsed.Add(item.Elapsed);
                }
            }
            return availableCount == 0 ? TimeSpan.MaxValue : availableElapsed / availableCount;
        }


        /// <summary>
        /// 添加扫描记录
        /// </summary>
        /// <param name="elapsed">扫描耗时</param>
        public void AddScanLog(TimeSpan elapsed)
        {
            var log = new ScanLog(this.Available, elapsed);
            this.history.Enqueue(log);
            while (this.history.Count > MAX_LOG_COUNT)
            {
                this.history.Dequeue();
            }
        }

        /// <summary>
        /// 是否相等
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(GithubContext? other)
        {
            return base.Equals(other);
        }

        /// <summary>
        /// 转换为统计信息
        /// </summary>
        /// <returns></returns>
        public string ToStatisticsString()
        {
            var availableRate = Math.Round(this.AvailableRate * 100, 2);
            return $"{{{nameof(Address)}={this.Address}, {nameof(AvailableRate)}={availableRate}%, {nameof(AvgElapsed)}={this.AvgElapsed.TotalSeconds}s}}";
        }
    }
}
