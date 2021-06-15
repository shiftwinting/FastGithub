using System.Collections.Generic;

namespace FastGithub.Scanner
{
    class GithubContextHashSet : HashSet<GithubContext>
    {
        public readonly object SyncRoot = new();
    }
}
