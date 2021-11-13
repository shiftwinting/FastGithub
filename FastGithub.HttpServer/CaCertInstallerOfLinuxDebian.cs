using Microsoft.Extensions.Logging;

namespace FastGithub.HttpServer
{
    sealed class CaCertInstallerOfLinuxDebian : CaCertInstallerOfLinux
    {
        protected override string CertToolName => "update-ca-certificates";

        protected override string CertStorePath => "/usr/local/share/ca-certificates";

        public CaCertInstallerOfLinuxDebian(ILogger<CaCertInstallerOfLinuxDebian> logger)
            : base(logger)
        {
        }
    }
}