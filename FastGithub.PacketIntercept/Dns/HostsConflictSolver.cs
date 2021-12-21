using FastGithub.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.PacketIntercept.Dns
{
    /// <summary>
    /// host文件冲解决者
    /// </summary>
    [SupportedOSPlatform("windows")]
    sealed class HostsConflictSolver : IDnsConflictSolver
    {
        private readonly FastGithubConfig fastGithubConfig;
        private readonly ILogger<HostsConflictSolver> logger;

        /// <summary>
        /// host文件冲解决者
        /// </summary>
        /// <param name="fastGithubConfig"></param>
        /// <param name="logger"></param>
        public HostsConflictSolver(
            FastGithubConfig fastGithubConfig,
            ILogger<HostsConflictSolver> logger)
        {
            this.fastGithubConfig = fastGithubConfig;
            this.logger = logger;
        }

        /// <summary>
        /// 解决冲突
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task SolveAsync(CancellationToken cancellationToken)
        {
            var hostsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers/etc/hosts");
            if (File.Exists(hostsPath) == false)
            {
                return;
            }

            Encoding hostsEncoding;
            var hasConflicting = false;
            var hostsBuilder = new StringBuilder();
            using (var fileStream = new FileStream(hostsPath, FileMode.Open, FileAccess.Read))
            {
                using var streamReader = new StreamReader(fileStream);
                while (streamReader.EndOfStream == false)
                {
                    var line = await streamReader.ReadLineAsync();
                    if (this.IsConflictingLine(line))
                    {
                        hasConflicting = true;
                        hostsBuilder.AppendLine($"# {line}");
                    }
                    else
                    {
                        hostsBuilder.AppendLine(line);
                    }
                }
                hostsEncoding = streamReader.CurrentEncoding;
            }


            if (hasConflicting == true)
            {
                try
                {
                    await File.WriteAllTextAsync(hostsPath, hostsBuilder.ToString(), hostsEncoding, cancellationToken);
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning($"无法解决hosts文件冲突：{ex.Message}");
                }
            }
        }

        /// <summary>
        /// 恢复冲突
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task RestoreAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 是否为冲突的行
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private bool IsConflictingLine(string? line)
        {
            if (line == null || line.TrimStart().StartsWith("#"))
            {
                return false;
            }

            var items = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (items.Length < 2)
            {
                return false;
            }

            var domain = items[1];
            return this.fastGithubConfig.IsMatch(domain);
        }
    }
}
