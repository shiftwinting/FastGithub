using System;
using System.Net.Http;

namespace FastGithub.ReverseProxy
{
    /// <summary>
    /// SniContext扩展
    /// </summary>
    static class SniContextExtensions
    {
        private static readonly HttpRequestOptionsKey<SniContext> key = new(nameof(SniContext));

        /// <summary>
        /// 设置SniContext
        /// </summary>
        /// <param name="httpRequestMessage"></param>
        /// <param name="context"></param>
        public static void SetSniContext(this HttpRequestMessage httpRequestMessage, SniContext context)
        {
            httpRequestMessage.Options.Set(key, context);
        }

        /// <summary>
        /// 获取SniContext
        /// </summary>
        /// <param name="httpRequestMessage"></param>
        /// <returns></returns>
        public static SniContext GetSniContext(this HttpRequestMessage httpRequestMessage)
        {
            if (httpRequestMessage.Options.TryGetValue(key, out var value))
            {
                return value;
            }
            throw new InvalidOperationException($"请先调用{nameof(SetSniContext)}");
        }
    }
}
