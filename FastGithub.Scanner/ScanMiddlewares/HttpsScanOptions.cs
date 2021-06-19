using System;

namespace FastGithub.Scanner.ScanMiddlewares
{
    [Options("Github:Scan:HttpsScan")]
    sealed class HttpsScanOptions
    {
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5d);
    }
}
