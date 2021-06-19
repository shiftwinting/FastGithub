using System;

namespace FastGithub.Scanner.LookupProviders
{
    [Options("Github:Lookup:PublicDnsProvider")]
    sealed class PublicDnsProviderOptions
    {
        public bool Enable { get; set; } = true;

        public string[] Dnss { get; set; } = Array.Empty<string>();
    }
}
