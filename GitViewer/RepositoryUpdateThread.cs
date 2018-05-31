using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GitViewer
{
    public class RepositoryUpdateThread
    {
        public class Result
        {
            public List<GitRevision> Commits { get; }
            public List<GitReference> Branches { get; }
            public string CurrentBranch { get; }

            public Result(List<GitRevision> commits, List<GitReference> branches, string currentBranch)
            {
                Commits = commits;
                Branches = branches;
                CurrentBranch = currentBranch;
            }
        }

        private Git git;

        public delegate void RepositoryUpdatedDelegate(Result result);
        private RepositoryUpdatedDelegate completionCallback;

        public RepositoryUpdateThread(string gitRepo, RepositoryUpdatedDelegate completionCallback)
        {
            git = new Git("git.exe", gitRepo);
            this.completionCallback = completionCallback;
        }

        private void WorkerThread()
        {
            List<GitRevision> commits = git.GetAllRevisions();
            Console.WriteLine("Found " + commits.Count + " commits.");

            List<GitReference> branches = git.GetAllReferences();
            Console.WriteLine("Found " + branches.Count + " branches.");

            string currentBranch = git.GetCurrentBranch();
            if (currentBranch != null)
            {
                Console.WriteLine("Current branch: " + currentBranch);
            }
            else
            {
                Console.WriteLine("Unknown current branch.");
            }

            var result = new Result(commits, branches, currentBranch);
            completionCallback(result);
        }

        public void BeginUpdateRepository()
        {
            var thread = new Thread(new ThreadStart(WorkerThread));
            thread.Start();
        }
    }
}
