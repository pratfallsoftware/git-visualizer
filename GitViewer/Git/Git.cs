using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GitViewer
{
    public class Git
    {
        private GitDiffCache diffCache = new GitDiffCache();
        private string gitCommand;
        private string repositoryPath;

        public Git(string gitCommand, string repositoryPath)
        {
            this.gitCommand = gitCommand;
            this.repositoryPath = repositoryPath;
        }

        public List<GitRevision> GetAllRevisions()
        {
            // --topo-order - Show no parents before all of its children are shown.  The graphing algorithm assumes this is true.
            // --reflog - Pretend as if all objects mentioned by reflogs are listed on the command line as <commit>.
            // --all - Pretend as if all the refs in refs/, along with HEAD, are listed on the command line as <commit>.
            // %H - commit hash
            // %P - parent hash
            // %s - commit subject
            // %ci - committer date: 2018-04-23 11:48:59 -0700
            // %an - author name
            string output = this.RunGitCommand("rev-list --all --reflog --topo-order --pretty=\"format:%H|**|%P|**|%ci|**|%an|**|%s\"");

            string[] outputLines = output.Split('\n');

            List <GitRevision> commits = new List<GitRevision>();
            foreach (string outputLine in outputLines)
            {
                if (outputLine.Length == 0)
                {
                    continue;
                }

                string[] outputLineParts = outputLine.Split(new string[] { "|**|" }, StringSplitOptions.None);
                string hash = outputLineParts[0];
                // Each commit has a "header" line that looks like "commit 1234567890..." followed by the actual formatted line.
                if (hash.StartsWith("commit"))
                {
                    continue;
                }

                string[] parentHashes;
                if (outputLineParts[1].Length > 0)
                {
                    parentHashes = outputLineParts[1].Split(' ');
                }
                else
                {
                    parentHashes = new string[0];
                }

                // We can cache the diff -- we know if the commit hash hasn't changed, the diff couldn't have either.
                string diffStr;
                if (diffCache.HasCachedDiff(hash))
                {
                    diffStr = diffCache.GetCachedDiff(hash);
                }
                else
                {
                    diffStr = GetContextlessDiff(hash);
                    diffCache.Add(hash, diffStr);
                }
                GitDiff diff = new GitDiff(diffStr);
                Author author = new Author(outputLineParts[3]);
                DateTime committerDate = DateTime.Parse(outputLineParts[2]);
                string description = outputLineParts[4];

                commits.Add(new GitRevision(hash, description, diff, author, committerDate, parentHashes));

            }
            return commits;
        }

        public void CheckOut(string entityToCheckOut)
        {
            this.RunGitCommand("checkout " + entityToCheckOut);
        }

        public string GetCurrentBranch()
        {
            string output = this.RunGitCommand("status");
            string[] outputLines = output.Split('\n');

            Regex onBranchRegex = new Regex("^On branch (.+)$", RegexOptions.IgnoreCase);
            Regex detachedHeadRegex = new Regex("^HEAD detached at (.+)$", RegexOptions.IgnoreCase);
            foreach (var outputLine in outputLines)
            {
                var match = onBranchRegex.Match(outputLine);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }

                match = detachedHeadRegex.Match(outputLine);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }

            return null;
        }

        public List<GitReference> SimulateReferencesToUnreachableCommits()
        {
            string output = this.RunGitCommand("fsck --full --no-reflogs --unreachable");
            string[] outputLines = output.Split('\n');

            List<GitReference> branches = new List<GitReference>();
            foreach (var outputLine in outputLines)
            {
                if (outputLine == "")
                {
                    continue;
                }
                string[] outputLineParts = outputLine.Split(' ');
                string type = outputLineParts[1];
                string hash = outputLineParts[2];

                if (type == "commit")
                {
                    branches.Add(new GitReference("[unreachable]", hash));
                }
            }

            return branches;
        }

        public List<GitReference> GetAllReferences()
        {
            string output = this.RunGitCommand("show-ref");
            string[] outputLines = output.Split('\n');

            List<GitReference> refs = new List<GitReference>();
            foreach (var outputLine in outputLines)
            {
                if (outputLine == "")
                {
                    continue;
                }
                string[] outputLineParts = outputLine.Split(' ');
                string hash = outputLineParts[0];
                string refFullPath = outputLineParts[1];

                refs.Add(new GitReference(refFullPath, hash));
            }

            return refs;
        }

        public List<GitReference> GetAllHeads()
        {
            string output = this.RunGitCommand("show-ref --heads");
            string[] outputLines = output.Split('\n');

            List<GitReference> branches = new List<GitReference>();
            foreach (var outputLine in outputLines)
            {
                if (outputLine == "")
                {
                    continue;
                }
                string[] outputLineParts = outputLine.Split(' ');
                string hash = outputLineParts[0];
                string headRef = outputLineParts[1];

                //We have "refs/heads/master".  We want "master"
                string[] headRefParts = headRef.Split('/');
                string branchName = headRefParts[2];

                branches.Add(new GitReference(branchName, hash));
            }

            return branches;
        }

        public string GetContextlessDiff(string commitHash)
        {
            // TODO Escape commitHash
            //--unified=0 sets 0 lines of context for each diff.
            //--no-pager says not to use "less" to let you scroll up and down, which would cause this to block.
            string output = this.RunGitCommand("--no-pager diff --unified=0 " + commitHash + "~1 " + commitHash);
            return output;
        }

        public string RunGitCommand(string commandArgs)
        {
            string oldDirectory = System.IO.Directory.GetCurrentDirectory();
            try
            {
                System.IO.Directory.SetCurrentDirectory(this.repositoryPath);

                ProcessStartInfo startInfo = new ProcessStartInfo(gitCommand, commandArgs);
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.CreateNoWindow = true;
                using (Process process = new Process())
                {
                    process.StartInfo = startInfo;

                    process.Start();
                    // For multi-page diffs, the process will not exit until the output has been read.
                    string output = "";
                    while (true)
                    {
                        string outputForThisLoop = process.StandardOutput.ReadToEnd();
                        output += outputForThisLoop;
                        if (outputForThisLoop == "")
                        {
                            break;
                        }
                        //System.Threading.Thread.Sleep(10);
                    }
                    process.WaitForExit();
                    output += process.StandardOutput.ReadToEnd();
                    return output;
                }
            }
            finally
            {
                System.IO.Directory.SetCurrentDirectory(oldDirectory);
            }
        }
    }
}
