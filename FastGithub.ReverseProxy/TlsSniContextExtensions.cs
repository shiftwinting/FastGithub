using System;
using System.Net.Http;

namespace FastGithub.ReverseProxy
{
    /// <summary>
    /// SniContext扩展
    /// </summary>
    static class TlsSniContextExtensions
    {
        private static readonly HttpRequestOptionsKey<TlsSniContext> key = new(nameof(TlsSniContext));

        /// <summary>
        /// 设置TlsSniContext
        /// </summary>
        /// <param name="httpRequestMessage"></param>
        /// <param name="context"></param>
        public static void SetTlsSniContext(this HttpRequestMessage httpRequestMessage, TlsSniContext context)
        {
            httpRequestMessage.Options.Set(key, context);
        }

        /// <summary>
        /// 获取TlsSniContext
        /// </summary>
        /// <param name="httpRequestMessage"></param>
        /// <returns></returns>
        public static TlsSniContext GetTlsSniContext(this HttpRequestMessage httpRequestMessage)
        {
            if (httpRequestMessage.Options.TryGetValue(key, out var value))
            {
                return value;
            }
            throw new InvalidOperationException($"请先调用{nameof(SetTlsSniContext)}");
        }
    }
}
