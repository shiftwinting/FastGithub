using System.Windows.Forms;

namespace FastGithub.Windows.Hosting
{
    /// <summary>
    /// 表示Application选项
    /// </summary>
    public class ApplicationOptions
    {
        /// <summary>
        /// 获取或设置是否启用VisualStyles
        /// </summary>
        public bool EnableVisualStyles { get; set; } = true;

        /// <summary>
        /// 获取或设置高Dpi的模式
        /// </summary>
        public HighDpiMode HighDpiMode { get; set; } = HighDpiMode.SystemAware;

        /// <summary>
        /// 获取或设置是否兼容TextRendering
        /// </summary>
        public bool CompatibleTextRenderingDefault { get; set; } = false;
    }
}
