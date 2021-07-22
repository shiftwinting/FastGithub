using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.DnscryptProxy
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
        public static Task SetListensAsync(string tomlPath, IPEndPoint endpoint, CancellationToken cancellationToken = default)
        {
            return SetAsync(tomlPath, "listen_addresses", $"['{endpoint}']");
        }

        /// <summary>
        /// 设置指定键的值
        /// </summary>
        /// <param name="tomlPath"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static async Task SetAsync(string tomlPath, string key, object? value, CancellationToken cancellationToken = default)
        {
            var lines = await File.ReadAllLinesAsync(tomlPath, cancellationToken);
            var toml = Set(lines, key, value);
            await File.WriteAllTextAsync(tomlPath, toml, cancellationToken);
        }

        /// <summary>
        /// 设置值
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static string Set(string[] lines, string key, object? value)
        {
            var updated = false;
            var builder = new StringBuilder();

            foreach (var line in lines)
            {
                var span = line.AsSpan();
                if (span.IsEmpty || span[0] == '#')
                {
                    builder.AppendLine(line);
                    continue;
                }

                var index = span.IndexOf('=');
                if (index <= 0 || span.Slice(0, index).Trim().SequenceEqual(key) == false)
                {
                    builder.AppendLine(line);
                    continue;
                }

                builder.Append(key).Append(" = ").AppendLine(value?.ToString());
                updated = true;
            }

            if (updated == false)
            {
                builder.Append(key).Append(" = ").AppendLine(value?.ToString());
            }
            return builder.ToString();
        }
    }
}
