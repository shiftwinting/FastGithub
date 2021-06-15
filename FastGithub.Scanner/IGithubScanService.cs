using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Scanner
{
    public interface IGithubScanService
    {
        Task ScanAllAsync(CancellationToken cancellationToken = default);
        Task ScanResultAsync(); 
        IPAddress? FindFastAddress(string domain);
    }
}