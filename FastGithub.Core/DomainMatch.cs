using System.Text.RegularExpressions;

namespace FastGithub
{
    /// <summary>
    /// 域名匹配
    /// </summary>
    sealed class DomainMatch
    {
        private readonly Regex regex;
        private readonly string domainPattern;

        /// <summary>
        /// 域名匹配
        /// </summary>
        /// <param name="domainPattern">域名表达式</param>
        public DomainMatch(string domainPattern)
        {
            this.domainPattern = domainPattern;
            var regexPattern = Regex.Escape(domainPattern).Replace(@"\*", ".*");
            this.regex = new Regex($"^{regexPattern}$", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// 是否与指定域名匹配
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        public bool IsMatch(string domain)
        {
            return this.regex.IsMatch(domain);
        }

        /// <summary>
        /// 转换为文本
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.domainPattern;
        }
    }
}
