using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Scanner
{
    public interface IGithubScanService
    {
        Task ScanAllAsync(CancellationToken cancellationToken);

        Task ScanResultAsync();

        bool IsAvailable(string domain, IPAddress address);

        IPAddress? FindBestAddress(string domain);
    }
}