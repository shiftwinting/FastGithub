using System;

namespace FastGithub.HttpServer
{
    class CaCertInstallerOfLinuxCentOS : CaCertInstallerOfLinux
    {
        public override string RootCertPath => "/etc/pki/ca-trust/source/anchors";

        public override string CertUpdateFileName => "update-ca-trust";

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