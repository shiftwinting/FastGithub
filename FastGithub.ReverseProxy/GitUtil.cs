using System;
using System.Diagnostics;

namespace FastGithub.ReverseProxy
{
    /// <summary>
    /// git工具
    /// </summary>
    static class GitUtil
    {
        /// <summary>
        /// 设置ssl验证
        /// </summary>
        /// <param name="value">是否验证</param>
        public static void SetSslverify(bool value)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = $"config --global http.sslverify {value.ToString().ToLower()}",
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
            }
            catch (Exception)
            {
            }
        }
    }
}
