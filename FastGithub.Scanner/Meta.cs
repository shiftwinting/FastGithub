using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text.Json.Serialization;

namespace FastGithub.Scanner
{
    sealed class Meta
    {
        [JsonPropertyName("hooks")]
        public string[] Hooks { get; set; } = Array.Empty<string>();

        [JsonPropertyName("web")]
        public string[] Web { get; set; } = Array.Empty<string>();

        [JsonPropertyName("api")]
        public string[] Api { get; set; } = Array.Empty<string>();

        [JsonPropertyName("git")]
        public string[] Git { get; set; } = Array.Empty<string>();

        [JsonPropertyName("packages")]
        public string[] Packages { get; set; } = Array.Empty<string>();

        [JsonPropertyName("pages")]
        public string[] Pages { get; set; } = Array.Empty<string>();

        [JsonPropertyName("importer")]
        public string[] Importer { get; set; } = Array.Empty<string>();

        [JsonPropertyName("actions")]
        public string[] Actions { get; set; } = Array.Empty<string>();

        [JsonPropertyName("dependabot")]
        public string[] Dependabot { get; set; } = Array.Empty<string>();


        public IEnumerable<GithubContext> ToGithubContexts()
        {
            foreach (var range in IPRange.From(this.Web).OrderBy(item => item.Size))
            {
                if (range.AddressFamily == AddressFamily.InterNetwork)
                {
                    foreach (var address in range)
                    {
                        yield return new GithubContext("github.com", address);
                    }
                }
            }

            foreach (var range in IPRange.From(this.Api).OrderBy(item => item.Size))
            {
                if (range.AddressFamily == AddressFamily.InterNetwork)
                {
                    foreach (var address in range)
                    {
                        yield return new GithubContext("api.github.com", address);
                    }
                }
            }
        }
    }
}
