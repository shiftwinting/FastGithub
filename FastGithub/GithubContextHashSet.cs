using System.Collections.Generic;

namespace FastGithub
{
    class GithubContextHashSet : HashSet<GithubContext>
    {
        public readonly object SyncRoot = new();
    }
}
