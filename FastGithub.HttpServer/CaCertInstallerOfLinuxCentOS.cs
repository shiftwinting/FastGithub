using System;

namespace FastGithub.HttpServer
{
    sealed class CaCertInstallerOfLinuxCentOS : CaCertInstallerOfLinuxRedHat
    {
        /// <summary>
        /// 是否支持
        /// </summary>
        /// <returns></returns>
        public override bool IsSupported()
        {
            return OperatingSystem.IsLinux() && base.IsReleasName("CentOS");
        }
    }
}