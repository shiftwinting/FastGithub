using FastGithub.Windows.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// WinForm生命周期
    /// </summary>
    sealed class WinFormHostLifetime : IHostLifetime, IDisposable
    {
        private readonly IHostApplicationLifetime applicationLifetime;
        private readonly IOptions<ApplicationOptions> applicationOptions;

        public WinFormHostLifetime(IHostApplicationLifetime applicationLifetime, IOptions<ApplicationOptions> applicationOptions)
        {
            this.applicationLifetime = applicationLifetime;
            this.applicationOptions = applicationOptions;
        }

        public Task WaitForStartAsync(CancellationToken cancellationToken)
        {
            var option = this.applicationOptions.Value;
            if (option.EnableVisualStyles == true)
            {
                Application.EnableVisualStyles();
            }

            Application.SetHighDpiMode(option.HighDpiMode);
            Application.SetCompatibleTextRenderingDefault(option.CompatibleTextRenderingDefault);

            Application.ApplicationExit += OnApplicationExit;
            return Task.CompletedTask;
        }

        private void OnApplicationExit(object? sender, System.EventArgs e)
        {
            Application.ApplicationExit -= OnApplicationExit;
            this.applicationLifetime.StopApplication();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            Application.ApplicationExit -= OnApplicationExit;
        }
    }
}
