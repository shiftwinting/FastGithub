using System;

namespace FastGithub
{
    /// <summary>
    /// 表示选项特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class OptionsAttribute : Attribute
    {
        public string? SessionKey { get; }

        /// <summary>
        /// 选项特性
        /// </summary>
        public OptionsAttribute()
        {
        }

        /// <summary>
        /// 选项特性
        /// </summary>
        /// <param name="sessionKey"></param>
        public OptionsAttribute(string sessionKey)
        {
            this.SessionKey = sessionKey;
        }
    }
}
