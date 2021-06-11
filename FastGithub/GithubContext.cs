using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace FastGithub
{
    class GithubContext
    {
        [AllowNull]
        public IPAddress Address { get; set; }

        public TimeSpan? HttpElapsed { get; set; }
    }
}
