using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;

namespace FastGithub.Scanner
{
    /// <summary>
    /// HttpClient工厂
    /// </summary>
    [Service(ServiceLifetime.Singleton)]
    sealed class HttpClientFactory
    {
        /// <summary>
        /// 程序集版本信息
        /// </summary>
        private static readonly AssemblyName assemblyName = typeof(HttpClientFactory).Assembly.GetName();

        /// <summary>
        /// 请求头的默认UserAgent
        /// </summary>
        private readonly static ProductInfoHeaderValue defaultUserAgent = new(assemblyName.Name ?? "FastGithub", assemblyName.Version?.ToString());

        /// <summary>
        /// 创建httpClient
        /// </summary>
        /// <returns></returns>
        public HttpClient Create(bool allowAutoRedirect = true)
        {
            var httpClient = new HttpClient(new HttpClientHandler
            {
                Proxy = null,
                UseProxy = false,
                AllowAutoRedirect = allowAutoRedirect
            });
            httpClient.DefaultRequestHeaders.Accept.TryParseAdd("*/*");
            httpClient.DefaultRequestHeaders.UserAgent.Add(defaultUserAgent);
            return httpClient;
        }
    }
}
