using FastGithub.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace FastGithub.Dns
{
    /// <summary>
    /// host文件冲突验证器
    /// </summary>
    sealed class HostsConflictValidator : IConflictValidator
    {
        private readonly FastGithubConfig fastGithubConfig;
        private readonly ILogger<HostsConflictValidator> logger;

        /// <summary>
        /// host文件冲突验证器
        /// </summary>
        /// <param name="fastGithubConfig"></param>
        /// <param name="logger"></param>
        public HostsConflictValidator(
            FastGithubConfig fastGithubConfig,
            ILogger<HostsConflictValidator> logger)
        {
            this.fastGithubConfig = fastGithubConfig;
            this.logger = logger;
        }

        /// <summary>
        /// 验证冲突 
        /// </summary>
        /// <returns></returns>
        public async Task ValidateAsync()
        {
            var hostsPath = @"/etc/hosts";
            if (OperatingSystem.IsWindows())
            {
                hostsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), $"drivers/{hostsPath}");
            }

            if (File.Exists(hostsPath) == false)
            {
                return;
            }

            var localAddresses = LocalMachine.GetAllIPv4Addresses().ToArray();
            var lines = await File.ReadAllLinesAsync(hostsPath);
            foreach (var line in lines)
            {
                if (HostsRecord.TryParse(line, out var record) == false)
                {
                    continue;
                }
                if (localAddresses.Contains(record.Address) == true)
                {
                    continue;
                }
                if (this.fastGithubConfig.IsMatch(record.Domain))
                {
                    this.logger.LogError($"由于你的hosts文件设置了{record}，{nameof(FastGithub)}无法加速此域名");
                }
            }
        }

        /// <summary>
        /// hosts文件记录
        /// </summary>
        private class HostsRecord
        {
            /// <summary>
            /// 获取域名
            /// </summary>
            public string Domain { get; }

            /// <summary>
            /// 获取地址
            /// </summary>
            public IPAddress Address { get; }

            private HostsRecord(string domain, IPAddress address)
            {
                this.Domain = domain;
                this.Address = address;
            }

            public override string ToString()
            {
                return $"[{this.Domain}->{this.Address}]";
            }

            public static bool TryParse(string record, [MaybeNullWhen(false)] out HostsRecord value)
            {
                value = null;
                if (record.TrimStart().StartsWith("#"))
                {
                    return false;
                }

                var items = record.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (items.Length < 2)
                {
                    return false;
                }

                if (IPAddress.TryParse(items[0], out var address) == false)
                {
                    return false;
                }

                value = new HostsRecord(items[1], address);
                return true;
            }
        }
    }
}
