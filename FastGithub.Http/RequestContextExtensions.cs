using System;
using System.Net.Http;

namespace FastGithub.Http
{
    /// <summary>
    /// 请求上下文扩展
    /// </summary>
    static class RequestContextExtensions
    {
        private static readonly HttpRequestOptionsKey<RequestContext> key = new(nameof(RequestContext));

        /// <summary>
        /// 设置RequestContext
        /// </summary>
        /// <param name="httpRequestMessage"></param>
        /// <param name="requestContext"></param>
        public static void SetRequestContext(this HttpRequestMessage httpRequestMessage, RequestContext requestContext)
        {
            httpRequestMessage.Options.Set(key, requestContext);
        }

        /// <summary>
        /// 获取RequestContext
        /// </summary>
        /// <param name="httpRequestMessage"></param>
        /// <returns></returns>
        public static RequestContext GetRequestContext(this HttpRequestMessage httpRequestMessage)
        {
            return httpRequestMessage.Options.TryGetValue(key, out var requestContext)
                ? requestContext
                : throw new InvalidOperationException($"请先调用{nameof(SetRequestContext)}");
        }
    }
}
