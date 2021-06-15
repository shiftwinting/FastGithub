using System;
using System.Net;

namespace FastGithub
{
    class GithubContext
    {
        public string Domain { get;  }

        public IPAddress Address { get;  }

        public TimeSpan? HttpElapsed { get; set; }

        public GithubContext(string domain,IPAddress address)
        {
            this.Domain = domain;
            this.Address = address;
        }

        public override string ToString()
        {
            return $"{Address}\t{Domain}\t# {HttpElapsed}";
        }
    }
}
