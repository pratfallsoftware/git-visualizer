using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitViewer
{
    public class GitRevision
    {
        public string Hash { get; }
        public GitDiff Diff { get; }
        public Author Author { get; }
        public DateTime CommitterDate { get; }
        public List<string> ParentHashes { get; }
        public string Description { get; }
        public bool IsMergeCommit
        {
            get
            {
                return (ParentHashes.Count > 1);
            }
        }

        public GitRevision(string hash, string description, GitDiff diff, Author author, DateTime committerDate, ICollection<string> parentHashes)
        {
            this.Hash = hash;
            this.Description = description;
            this.Diff = diff;
            this.Author = author;
            this.CommitterDate = committerDate;

            this.ParentHashes = new List<string>(parentHashes);
        }
    }
}
