using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitViewer
{
    class GitDiffCache
    {
        private Dictionary<string, string> diffByCommitHash = new Dictionary<string, string>();

        public void Add(string commitHash, string diff)
        {
            diffByCommitHash.Add(commitHash, diff);
        }

        public bool HasCachedDiff(string commitHash)
        {
            return diffByCommitHash.ContainsKey(commitHash);
        }

        public string GetCachedDiff(string commitHash)
        {
            return diffByCommitHash[commitHash];
        }
    }
}
