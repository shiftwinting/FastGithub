using System.Collections.Generic;

namespace FastGithub.Scanner
{
    sealed class GithubContextHashSet : HashSet<GithubContext>
    {
        public readonly object SyncRoot = new();
    }
}
