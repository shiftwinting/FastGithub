using Microsoft.Extensions.Logging;
using System;

namespace FastGithub.HttpServer
{
    sealed class CaCertInstallerOfMacOS : ICaCertInstaller
    {
        /// <summary>
        /// 是否支持
        /// </summary>
        /// <returns></returns>
        public bool IsSupported()
        {
            return OperatingSystem.IsMacOS();
        }

        /// <summary>
        /// 安装ca证书
        /// </summary>
        /// <param name="caCertFilePath">证书文件路径</param>
        /// <param name="logger"></param>
        public void Install(string caCertFilePath, ILogger logger)
        {
            logger.LogWarning($"请手动安装CA证书然后设置信任CA证书{caCertFilePath}");
        }
    }
}
