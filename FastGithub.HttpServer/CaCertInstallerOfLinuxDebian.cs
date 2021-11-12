using System;

namespace FastGithub.HttpServer
{
    class CaCertInstallerOfLinuxDebian : CaCertInstallerOfLinux
    {
        public override string RootCertPath => "/usr/local/share/ca-certificates";

        public override string CertUpdateFileName => "update-ca-certificates";

        /// <summary>
        /// 是否支持
        /// </summary>
        /// <returns></returns>
        public override bool IsSupported()
        {
            return OperatingSystem.IsLinux() && IsReleasName("Debian");
        }
    }
}