using FastGithub.Configuration;
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

        /// <summary>
        /// host文件冲解决者
        /// </summary>
        /// <param name="fastGithubConfig"></param>
        public HostsConflictSolver(FastGithubConfig fastGithubConfig)
        {
            this.fastGithubConfig = fastGithubConfig;
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

            var hasConflicting = false;
            var hostsBuilder = new StringBuilder();
            var lines = await File.ReadAllLinesAsync(hostsPath, cancellationToken);
            foreach (var line in lines)
            {
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

            if (hasConflicting == true)
            {
                File.SetAttributes(hostsPath, FileAttributes.Normal);
                await File.WriteAllTextAsync(hostsPath, hostsBuilder.ToString(), cancellationToken);
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
        private bool IsConflictingLine(string line)
        {
            if (line.TrimStart().StartsWith("#"))
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
