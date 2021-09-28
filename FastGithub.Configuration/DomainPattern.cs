using System;
using System.Text.RegularExpressions;

namespace FastGithub.Configuration
{
    /// <summary>
    /// 表示域名表达式
    /// *表示除.之外任意0到多个字符
    /// </summary>
    public class DomainPattern : IComparable<DomainPattern>
    {
        private readonly Regex regex;
        private readonly string domainPattern;

        /// <summary>
        /// 域名表达式
        /// *表示除.之外任意0到多个字符
        /// </summary>
        /// <param name="domainPattern">域名表达式</param>
        public DomainPattern(string domainPattern)
        {
            this.domainPattern = domainPattern;
            var regexPattern = Regex.Escape(domainPattern).Replace(@"\*", @"[^\.]*");
            this.regex = new Regex($"^{regexPattern}$", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// 与目标比较
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(DomainPattern? other)
        {
            if (other is null)
            {
                return 1;
            }

            var segmentsX = this.domainPattern.Split('.');
            var segmentsY = other.domainPattern.Split('.');
            var value = segmentsX.Length - segmentsY.Length;
            if (value != 0)
            {
                return value;
            }

            for (var i = segmentsX.Length - 1; i >= 0; i--)
            {
                var x = segmentsX[i];
                var y = segmentsY[i];

                value = Compare(x, y);
                if (value == 0)
                {
                    continue;
                }
                return value;
            }

            return 0;
        }


        /// <summary>
        ///  比较两个分段
        /// </summary>
        /// <param name="x">abc</param>
        /// <param name="y">abc*</param>
        /// <returns></returns>
        private static int Compare(string x, string y)
        {
            var valueX = x.Replace('*', char.MaxValue);
            var valueY = y.Replace('*', char.MaxValue);
            return valueX.CompareTo(valueY);
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
