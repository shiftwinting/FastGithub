using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Scanner
{
    /// <summary>
    /// github扫描结果
    /// </summary>
    [Service(ServiceLifetime.Singleton)]
    sealed class GithubScanResults
    {
        private const string dataFile = "FastGithub.dat";
        private readonly object syncRoot = new();
        private readonly List<GithubContext> contexts = new();
        private readonly ILogger<GithubScanResults> logger;

        /// <summary>
        /// github扫描结果
        /// </summary>
        /// <param name="logger"></param>
        public GithubScanResults(ILogger<GithubScanResults> logger)
        {
            this.logger = logger;
            var datas = LoadDatas(logger);
            foreach (var context in datas)
            {
                this.Add(context);
            }
        }

        /// <summary>
        /// 从磁盘加载数据
        /// </summary>
        /// <param name="logger"></param>
        /// <returns></returns>
        private static GithubContext[] LoadDatas(ILogger logger)
        {
            try
            {
                if (File.Exists(dataFile) == true)
                {
                    var json = File.ReadAllBytes(dataFile);
                    var datas = JsonSerializer.Deserialize<GithubDomainAddress[]>(json);
                    if (datas != null)
                    {
                        return datas.Select(item => item.ToGithubContext()).ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning($"从{dataFile}加载数据失败：{ex.Message}");
            }

            return Array.Empty<GithubContext>();
        }

        /// <summary>
        /// 添加GithubContext
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public bool Add(GithubContext context)
        {
            lock (this.syncRoot)
            {
                if (this.contexts.Contains(context))
                {
                    return false;
                }
                this.contexts.Add(context);
                return true;
            }
        }

        /// <summary>
        /// 转换为数组
        /// </summary>
        /// <returns></returns>
        public GithubContext[] ToArray()
        {
            lock (this.syncRoot)
            {
                return this.contexts.ToArray();
            }
        }

        /// <summary>
        /// 查找最优的ip
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        public IPAddress? FindBestAddress(string domain)
        {
            lock (this.syncRoot)
            {
                return this.contexts
                    .Where(item => item.Domain == domain)
                    .OrderByDescending(item => item.AvailableRate)
                    .ThenByDescending(item => item.Available)
                    .ThenBy(item => item.AvgElapsed)
                    .Select(item => item.Address)
                    .FirstOrDefault();
            }
        }

        /// <summary>
        /// 保存数据到磁盘
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task SaveDatasAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var stream = File.OpenWrite(dataFile);
                var datas = this.ToArray()
                    .OrderByDescending(item => item.AvailableRate)
                    .Select(item => GithubDomainAddress.From(item));

                await JsonSerializer.SerializeAsync(stream, datas, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                this.logger.LogWarning($"保存数据到{dataFile}失败：{ex.Message}");
            }
        }


        /// <summary>
        /// github的域名与ip关系数据
        /// </summary>
        private class GithubDomainAddress
        {
            [AllowNull]
            public string Domain { get; set; }

            [AllowNull]
            public string Address { get; set; }

            public GithubContext ToGithubContext()
            {
                return new GithubContext(this.Domain, IPAddress.Parse(this.Address)) { Available = true };
            }

            public static GithubDomainAddress From(GithubContext context)
            {
                return new GithubDomainAddress
                {
                    Domain = context.Domain,
                    Address = context.Address.ToString()
                };
            }
        }
    }
}
