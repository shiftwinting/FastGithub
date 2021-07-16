using System;

namespace FastGithub.ReverseProxy
{
    /// <summary>
    /// 反向代理异常
    /// </summary>
    sealed class ReverseProxyException : Exception
    {
        /// <summary>
        /// 反向代理异常
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public ReverseProxyException(string message, Exception? inner)
            : base(message, inner)
        {
        }
    }
}
