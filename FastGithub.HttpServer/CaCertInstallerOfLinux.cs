using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace FastGithub.HttpServer
{
    abstract class CaCertInstallerOfLinux : ICaCertInstaller
    {
        const string OS_RELEASE_FILE = "/etc/os-release";

        /// <summary>
        /// 更新工具文件名
        /// </summary>
        public abstract string CertUpdateFileName { get; }

        /// <summary>
        /// 证书根目录
        /// </summary>
        public abstract string RootCertPath { get; }

        /// <summary>
        /// 是否支持
        /// </summary>
        /// <returns></returns>
        public abstract bool IsSupported();

        /// <summary>
        /// 安装ca证书
        /// </summary>
        /// <param name="caCertFilePath">证书文件路径</param>
        /// <param name="logger"></param>
        public void Install(string caCertFilePath, ILogger logger)
        {
            var destCertFilePath = Path.Combine(this.RootCertPath, "fastgithub.crt");
            if (File.Exists(destCertFilePath) && File.ReadAllBytes(caCertFilePath).SequenceEqual(File.ReadAllBytes(destCertFilePath)))
            {
                return;
            }

            if (Environment.UserName != "root")
            {
                logger.LogWarning($"无法自动安装CA证书{caCertFilePath}，因为没有root权限");
                return;
            }

            try
            {
                Directory.CreateDirectory(this.RootCertPath);
                File.Copy(caCertFilePath, destCertFilePath, overwrite: true);
                Process.Start(this.CertUpdateFileName).WaitForExit();
            }
            catch (Exception ex)
            {
                File.Delete(destCertFilePath);
                logger.LogWarning(ex.Message, "自动安装证书异常");
            }
        }


        /// <summary>
        /// 是否为某个发行版
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        protected static bool IsReleasName(string name)
        {
            if (File.Exists(OS_RELEASE_FILE) == false)
            {
                return false;
            }

            foreach (var line in File.ReadAllLines(OS_RELEASE_FILE))
            {
                if (line.StartsWith("NAME=") && line.Contains(name))
                {
                    return true;
                }
            }

            return false;
        }
    }
}