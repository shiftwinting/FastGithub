using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.ReverseProxy
{
    /// <summary>
    /// 表示LifetimeHttpHandler清理器
    /// </summary>
    sealed class LifetimeHttpHandlerCleaner
    {
        private readonly ILogger logger;

        /// <summary>
        /// 当前监视生命周期的记录的数量
        /// </summary>
        private int trackingEntryCount = 0;

        /// <summary>
        /// 监视生命周期的记录队列
        /// </summary>
        private readonly ConcurrentQueue<TrackingEntry> trackingEntries = new();

        /// <summary>
        /// 获取或设置清理的时间间隔
        /// 默认10s
        /// </summary>
        public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromSeconds(10d);

        /// <summary>
        /// LifetimeHttpHandler清理器
        /// </summary>
        /// <param name="logger"></param>
        public LifetimeHttpHandlerCleaner(ILogger logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// 添加要清除的httpHandler
        /// </summary>
        /// <param name="handler">httpHandler</param>
        public void Add(LifetimeHttpHandler handler)
        {
            var entry = new TrackingEntry(handler);
            this.trackingEntries.Enqueue(entry);

            // 从0变为1，要启动清理作业
            if (Interlocked.Increment(ref this.trackingEntryCount) == 1)
            {
                this.StartCleanup();
            }
        }

        /// <summary>
        /// 启动清理作业
        /// </summary>
        private async void StartCleanup()
        {
            try
            {
                while (true)
                {
                    await Task.Delay(this.CleanupInterval);
                    if (this.Cleanup() == true)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "清理HttpMessageHandler出现不可预期的异常");
                // 这是应该不可能发生的
            }
        }

        /// <summary>
        /// 清理失效的拦截器
        /// 返回是否完全清理
        /// </summary>
        /// <returns></returns>
        private bool Cleanup()
        {
            var cleanCount = this.trackingEntries.Count;
            this.logger.LogTrace($"尝试清理{cleanCount}条HttpMessageHandler");

            for (var i = 0; i < cleanCount; i++)
            {
                this.trackingEntries.TryDequeue(out var entry);
                Debug.Assert(entry != null);

                if (entry.CanDispose == false)
                {
                    this.trackingEntries.Enqueue(entry);
                    continue;
                }

                this.logger.LogTrace($"释放了{entry.GetHashCode()}@HttpMessageHandler");
                entry.Dispose();
                if (Interlocked.Decrement(ref this.trackingEntryCount) == 0)
                {
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// 表示监视生命周期的记录
        /// </summary>
        private class TrackingEntry : IDisposable
        {
            /// <summary>
            /// 用于释放资源的对象
            /// </summary>
            private readonly IDisposable disposable;

            /// <summary>
            /// 监视对象的弱引用
            /// </summary>
            private readonly WeakReference weakReference;

            /// <summary>
            /// 获取是否可以释放资源
            /// </summary>
            /// <returns></returns>
            public bool CanDispose => this.weakReference.IsAlive == false;

            /// <summary>
            /// 监视生命周期的记录
            /// </summary>
            /// <param name="handler">激活状态的httpHandler</param>
            public TrackingEntry(LifetimeHttpHandler handler)
            {
                this.disposable = handler.InnerHandler!;
                this.weakReference = new WeakReference(handler);
            }

            /// <summary>
            /// 释放资源
            /// </summary>
            public void Dispose()
            {
                try
                {
                    this.disposable.Dispose();
                }
                catch (Exception) { }
            }
        }
    }
}
