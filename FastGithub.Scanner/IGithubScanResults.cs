using System.Net;

namespace FastGithub.Scanner
{
    public interface IGithubScanResults
    {
        bool IsAvailable(string domain, IPAddress address);

        IPAddress? FindBestAddress(string domain);
    }
}
