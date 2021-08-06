using FastGithub.Configuration;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.DomainResolve
{
    /// <summary>
    /// doml配置工具
    /// </summary>
    static class TomlUtil
    {
        /// <summary>
        /// 设置监听地址
        /// </summary>
        /// <param name="tomlPath"></param>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public static Task<bool> SetListensAsync(string tomlPath, IPEndPoint endpoint, CancellationToken cancellationToken = default)
        {
            return SetAsync(tomlPath, "listen_addresses", $"['{endpoint}']", cancellationToken);
        }

        /// <summary>
        /// 设置ecs
        /// </summary>
        /// <param name="tomlPath"></param> 
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<bool> SetEdnsClientSubnetAsync(string tomlPath, CancellationToken cancellationToken = default)
        {
            try
            {
                var address = await GetPublicIPAddressAsync(cancellationToken);
                return await SetAsync(tomlPath, "edns_client_subnet", @$"[""{address}/32""]", cancellationToken);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 获取公网ip
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private static async Task<IPAddress> GetPublicIPAddressAsync(CancellationToken cancellationToken)
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(3d) };
            var response = await httpClient.GetStringAsync("https://pv.sohu.com/cityjson?ie=utf-8", cancellationToken);
            var match = Regex.Match(response, @"\d+\.\d+\.\d+\.\d+");
            return match.Success && IPAddress.TryParse(match.Value, out var address)
                ? address
                : throw new FastGithubException("无法获取外网ip");
        }

        /// <summary>
        /// 设置指定键的值
        /// </summary>
        /// <param name="tomlPath"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static async Task<bool> SetAsync(string tomlPath, string key, object? value, CancellationToken cancellationToken = default)
        {
            var setted = false;
            var builder = new StringBuilder();
            var lines = await File.ReadAllLinesAsync(tomlPath, cancellationToken);

            foreach (var line in lines)
            {
                if (Regex.IsMatch(line, @$"(?<=#*\s*){key}(?=\s*=)") == false)
                {
                    builder.AppendLine(line);
                }
                else if (setted == false)
                {
                    setted = true;
                    builder.Append(key).Append(" = ").AppendLine(value?.ToString());
                }
            }

            var toml = builder.ToString();
            await File.WriteAllTextAsync(tomlPath, toml, cancellationToken);
            return setted;
        }
    }
}
