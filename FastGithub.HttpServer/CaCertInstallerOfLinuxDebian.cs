using Microsoft.Extensions.Logging;

namespace FastGithub.HttpServer
{
    sealed class CaCertInstallerOfLinuxDebian : CaCertInstallerOfLinux
    {
        protected override string CaCertUpdatePath => "/bin/update-ca-certificates";

        protected override string CaCertStorePath => "/usr/local/share/ca-certificates";

        public CaCertInstallerOfLinuxDebian(ILogger<CaCertInstallerOfLinuxDebian> logger)
            : base(logger)
        {
        }
    }
}