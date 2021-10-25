using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FastGithub.Windows.Hosting
{
    /// <summary>
    /// WinForm后台任务和WinForm线程
    /// </summary>
    /// <typeparam name="TMainForm"></typeparam>
    sealed class WinFormHostedService<TMainForm> : IHostedService where TMainForm : Form
    {
        private readonly Thread staThread;
        private readonly IServiceProvider serviceProvider;
        private readonly TaskCompletionSource taskCompletionSource = new();

        /// <summary>
        /// WinForm后台任务
        /// </summary>
        /// <param name="serviceProvider"></param>
        public WinFormHostedService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            this.staThread = new Thread(StaRunMainFrom);
            this.staThread.TrySetApartmentState(ApartmentState.STA);
        }

        /// <summary>
        /// 启动
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            this.staThread.Start();
            return this.taskCompletionSource.Task;
        }

        /// <summary>
        /// STA线程
        /// </summary>
        private void StaRunMainFrom()
        {
            try
            {
                var mainForm = this.CreateMainForm();
                this.taskCompletionSource.TrySetResult();
                Application.Run(mainForm);
            }
            catch (Exception ex)
            {
                this.taskCompletionSource.TrySetException(ex);
            }
        }

        /// <summary>
        /// 实例化MainForm与初始化调度器
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        private TMainForm CreateMainForm()
        {
            // 在STA线程实例化TMainForm，保证该线程拥有SynchronizationContext
            var mainForm = this.serviceProvider.GetRequiredService<TMainForm>();
            if (SynchronizationContext.Current is null)
            {
                throw new InvalidOperationException($"不允许在其它线程上实例化{typeof(TMainForm)}");
            }

            var dispatcher = this.serviceProvider.GetRequiredService<IWinFormDispatcher>();
            dispatcher.SynchronizationContext = SynchronizationContext.Current;
            return mainForm;
        }

        /// <summary>
        /// 停止
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            Application.Exit();
            return Task.CompletedTask;
        }
    }
}
