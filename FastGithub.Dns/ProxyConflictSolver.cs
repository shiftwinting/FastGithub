using FastGithub.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Dns
{
    /// <summary>
    /// 代理冲突解决者
    /// </summary>
    [SupportedOSPlatform("windows")]
    sealed class ProxyConflictSolver : IConflictSolver
    {
        private const int INTERNET_OPTION_REFRESH = 37;
        private const int INTERNET_OPTION_PROXY_SETTINGS_CHANGED = 95;

        private const string PROXYOVERRIDE_KEY = "ProxyOverride";
        private const string INTERNET_SETTINGS = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";

        private readonly IOptions<FastGithubOptions> options;
        private readonly ILogger<ProxyConflictSolver> logger;

        [DllImport("wininet.dll")]
        private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);


        /// <summary>
        /// 代理冲突解决者
        /// </summary>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public ProxyConflictSolver(
            IOptions<FastGithubOptions> options,
            ILogger<ProxyConflictSolver> logger)
        {
            this.options = options;
            this.logger = logger;
        }

        /// <summary>
        /// 解决冲突
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task SolveAsync(CancellationToken cancellationToken)
        {
            this.SetToProxyOvride();
            this.CheckProxyConflict();
            return Task.CompletedTask;
        }

        /// <summary>
        /// 恢复冲突
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task RestoreAsync(CancellationToken cancellationToken)
        {
            this.RemoveFromProxyOvride();
            return Task.CompletedTask;
        }

        /// <summary>
        /// 添加到ProxyOvride
        /// </summary>
        private void SetToProxyOvride()
        {
            using var settings = Registry.CurrentUser.OpenSubKey(INTERNET_SETTINGS, writable: true);
            if (settings == null)
            {
                return;
            }

            var items = this.options.Value.DomainConfigs.Keys.ToHashSet();
            foreach (var item in GetProxyOvride(settings))
            {
                items.Add(item);
            }
            SetProxyOvride(settings, items);
        }

        /// <summary>
        /// 从ProxyOvride移除
        /// </summary>
        private void RemoveFromProxyOvride()
        {
            using var settings = Registry.CurrentUser.OpenSubKey(INTERNET_SETTINGS, writable: true);
            if (settings == null)
            {
                return;
            }

            var proxyOvride = GetProxyOvride(settings);
            var items = proxyOvride.Except(this.options.Value.DomainConfigs.Keys);
            SetProxyOvride(settings, items);
        }

        /// <summary>
        /// 检测代理冲突
        /// </summary>
        private void CheckProxyConflict()
        {
            if (HttpClient.DefaultProxy == null)
            {
                return;
            }

            foreach (var domain in this.options.Value.DomainConfigs.Keys)
            {
                var destination = new Uri($"https://{domain.Replace('*', 'a')}");
                var proxyServer = HttpClient.DefaultProxy.GetProxy(destination);
                if (proxyServer != null)
                {
                    this.logger.LogError($"由于系统配置了{proxyServer}代理{domain}，{nameof(FastGithub)}无法加速相关域名");
                }
            }
        }

        /// <summary>
        /// 获取ProxyOverride
        /// </summary>
        /// <param name="registryKey"></param>
        /// <returns></returns>
        private static string[] GetProxyOvride(RegistryKey registryKey)
        {
            var value = registryKey.GetValue(PROXYOVERRIDE_KEY, null)?.ToString();
            return value == null ? Array.Empty<string>() : value.Split(';', StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// 设置ProxyOverride
        /// </summary>
        /// <param name="registryKey"></param>
        /// <param name="items"></param>
        private static void SetProxyOvride(RegistryKey registryKey, IEnumerable<string> items)
        {
            var value = string.Join(';', items);
            registryKey.SetValue(PROXYOVERRIDE_KEY, value, RegistryValueKind.String);
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_PROXY_SETTINGS_CHANGED, IntPtr.Zero, 0);
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);
        }
    }
}
