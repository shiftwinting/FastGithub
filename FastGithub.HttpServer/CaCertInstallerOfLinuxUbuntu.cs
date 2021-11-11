using System;

namespace FastGithub.HttpServer
{
    sealed class CaCertInstallerOfLinuxUbuntu : CaCertInstallerOfLinuxDebain
    {
        /// <summary>
        /// 是否支持
        /// </summary>
        /// <returns></returns>
        public override bool IsSupported()
        {
            return OperatingSystem.IsLinux() && base.IsReleasName("Ubuntu");
        }
    }
}