using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitViewer
{
    public class GitReference
    {
        public string CommitHash { get; }
        public string ShortName { get; }
        public string FullName { get; }
        public GitReferenceType Type { get; }
        public string RemoteName { get; }

        public GitReference(string fullName, string commitHash)
        {
            string[] nameParts = fullName.Split('/');
            string shortName = nameParts[nameParts.Length - 1];

            this.RemoteName = null;
            GitReferenceType type = GitReferenceType.Other;
            if (nameParts.Length > 1)
            {
                switch (nameParts[1])
                {
                    case "heads":
                        type = GitReferenceType.Head;
                        break;
                    case "remotes":
                        if (nameParts.Length == 4)
                        {
                            // Pull "origin" from "refs/remotes/origin/HEAD"
                            this.RemoteName = nameParts[2];
                        }
                        type = GitReferenceType.Remote;
                        break;
                    case "tags":
                        type = GitReferenceType.Tag;
                        break;
                    case "stash":
                        type = GitReferenceType.Stash;
                        break;
                    default:
                        break;
                }
            }

            this.ShortName = shortName;
            this.FullName = fullName;
            this.CommitHash = commitHash;
            this.Type = type;
        }
    }
}
