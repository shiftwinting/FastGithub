using System;

namespace FastGithub.Configuration
{
    /// <summary>
    /// 表示FastGithub异常
    /// </summary>
    public class FastGithubException : Exception
    {
        /// <summary>
        /// FastGithub异常
        /// </summary>
        /// <param name="message"></param>
        public FastGithubException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// FastGithub异常
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public FastGithubException(string message, Exception? innerException)
            : base(message, innerException)
        {
        }
    }
}
