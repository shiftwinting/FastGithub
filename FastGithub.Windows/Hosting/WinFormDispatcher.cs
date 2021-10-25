using System.Threading;

namespace FastGithub.Windows.Hosting
{
    /// <summary>
    /// WinForm调度器
    /// </summary>
    sealed class WinFormDispatcher : IWinFormDispatcher
    {
        /// <summary>
        /// 获取或设置同步上下文
        /// </summary>
        public SynchronizationContext? SynchronizationContext { get; set; }
    }
}
