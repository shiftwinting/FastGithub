using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tommy;

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
        public static Task SetListensAsync(string tomlPath, IPEndPoint endpoint, CancellationToken cancellationToken = default)
        {
            var value = new TomlArray
            {
                endpoint.ToString()
            };
            return SetAsync(tomlPath, "listen_addresses", value, cancellationToken);
        }

        /// <summary>
        /// 设置指定键的值
        /// </summary>
        /// <param name="tomlPath"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static async Task SetAsync(string tomlPath, string key, TomlNode value, CancellationToken cancellationToken = default)
        {
            var toml = await File.ReadAllTextAsync(tomlPath, cancellationToken);
            var reader = new StringReader(toml);
            var tomlTable = TOML.Parse(reader);
            tomlTable[key] = value;

            var builder = new StringBuilder();
            var writer = new StringWriter(builder);
            tomlTable.WriteTo(writer);
            toml = builder.ToString();

            await File.WriteAllTextAsync(tomlPath, toml, cancellationToken);
        }
    }
}
