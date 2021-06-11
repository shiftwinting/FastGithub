using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace FastGithub
{
    class GithubContext
    {
        [AllowNull]
        public string Domain { get; set; }

        [AllowNull]
        public IPAddress Address { get; set; }

        public TimeSpan? HttpElapsed { get; set; }

        public override string ToString()
        {
            return $"{Address}\t{Domain}\t# {HttpElapsed}";
        }
    }
}
