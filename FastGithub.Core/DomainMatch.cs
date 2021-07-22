using System;
using System.Text.RegularExpressions;

namespace FastGithub
{
    /// <summary>
    /// 域名匹配
    /// </summary>
    public class DomainMatch : IComparable<DomainMatch>
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
        /// 与目标比较
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(DomainMatch? other)
        {
            if (other is null)
            {
                return 1;
            }

            var segmentsX = this.domainPattern.Split('.');
            var segmentsY = other.domainPattern.Split('.');
            if (segmentsX.Length > segmentsY.Length)
            {
                return 1;
            }
            if (segmentsX.Length < segmentsY.Length)
            {
                return -1;
            }

            for (var i = segmentsX.Length - 1; i >= 0; i--)
            {
                var x = segmentsX[i];
                var y = segmentsY[i];

                var value = Compare(x, y);
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
            if (x == y)
            {
                return 0;
            }

            var valueX = x.Replace("*", null);
            var valueY = y.Replace("*", null);

            var maskX = x.Length - valueX.Length;
            var maskY = y.Length - valueY.Length;

            var value = maskX - maskY;
            if (value != 0)
            {
                return value;
            }

            value = valueX.CompareTo(valueY);
            if (value == 0)
            {
                value = x.CompareTo(y);
            }
            return value;
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
