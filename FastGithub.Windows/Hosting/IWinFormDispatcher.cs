using System;
using System.Threading;

namespace FastGithub.Windows.Hosting
{
    /// <summary>
    /// WinForm调度器
    /// </summary>
    public interface IWinFormDispatcher
    {
        /// <summary>
        /// 获取或设置同步上下文
        /// </summary>
        SynchronizationContext? SynchronizationContext { get; set; }

        /// <summary>
        /// 尝试在同步上下文投递执行委托
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        bool TryInvoke(Action action)
        {
            var context = this.SynchronizationContext;
            if (context == null || action == null)
            {
                return false;
            }

            context.Post(state => ((Action)state!)(), action);
            return false;
        }

        /// <summary>
        /// 在同步上下文投递执行委托
        /// </summary>
        /// <param name="action"></param>
        void Invoke(Action action)
        {
            var context = this.SynchronizationContext;
            if (context == null)
            {
                throw new InvalidOperationException($"{nameof(SynchronizationContext)} is null");
            }

            if (action != null)
            {
                context.Post(state => ((Action)state!)(), action);
            }
        }
    }
}
