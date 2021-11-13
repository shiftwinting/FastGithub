using Microsoft.Extensions.Logging;

namespace FastGithub.HttpServer
{
    sealed class CaCertInstallerOfLinuxRedHat : CaCertInstallerOfLinux
    {
        protected override string CertToolName => "update-ca-trust";

        protected override string CertStorePath => "/etc/pki/ca-trust/source/anchors";

        public CaCertInstallerOfLinuxRedHat(ILogger<CaCertInstallerOfLinuxRedHat> logger)
            : base(logger)
        {
        }
    }
}