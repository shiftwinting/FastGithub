using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json.Serialization;

namespace FastGithub
{
    class Meta
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


        public IEnumerable<IPAddress> ToIPv4Address()
        {
            var cidrs = this.Web.Concat(this.Api);
            foreach (var cidr in cidrs)
            {
                if (IPv4CIDR.TryParse(cidr, out var value))
                {
                    foreach (var ip in value.GetAllIPAddress())
                    {
                        yield return ip;
                    }
                }
            }
        }
    }
}
