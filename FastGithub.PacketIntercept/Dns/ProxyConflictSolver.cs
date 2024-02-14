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

namespace FastGithub.PacketIntercept.Dns
{
    /// <summary>
    /// 代理冲突解决者
    /// </summary>
    [SupportedOSPlatform("windows")]
    sealed class ProxyConflictSolver : IDnsConflictSolver
    {
        private const int INTERNET_OPTION_REFRESH = 37;
        private const int INTERNET_OPTION_PROXY_SETTINGS_CHANGED = 95;

        private const char PROXYOVERRIDE_SEPARATOR = ';';
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
            var systemProxy = HttpClient.DefaultProxy;
            if (systemProxy == null)
            {
                return;
            }

            foreach (var domain in this.options.Value.DomainConfigs.Keys)
            {
                var destination = new Uri($"https://{domain.Replace('*', 'a')}");
                var proxyServer = systemProxy.GetProxy(destination);
                if (proxyServer != null)
                {
                    this.logger.LogError($"由于系统设置了代理{proxyServer}，{nameof(FastGithub)}无法加速{domain}");
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
            if (value == null)
            {
                return Array.Empty<string>();
            }

            return value
                .Split(PROXYOVERRIDE_SEPARATOR, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim())
                .ToArray();
        }

        /// <summary>
        /// 设置ProxyOverride
        /// </summary>
        /// <param name="registryKey"></param>
        /// <param name="items"></param>
        private static void SetProxyOvride(RegistryKey registryKey, IEnumerable<string> items)
        {
            var value = string.Join(PROXYOVERRIDE_SEPARATOR, items);
            registryKey.SetValue(PROXYOVERRIDE_KEY, value, RegistryValueKind.String);
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_PROXY_SETTINGS_CHANGED, IntPtr.Zero, 0);
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);
        }
    }
}
