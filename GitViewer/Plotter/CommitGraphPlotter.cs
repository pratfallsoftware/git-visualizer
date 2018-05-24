using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace GitViewer
{
    public class CommitGraphPlotter
    {
        private Dictionary<string, GitRevision> commitsByHash = new Dictionary<string, GitRevision>();
        private string[] commitHashes = new string[0];
        private Dictionary<string, Point> cellNumberByHash = new Dictionary<string, Point>();
        private Dictionary<int, List<int>> listOfUsedRowsByColumn = new Dictionary<int, List<int>>();
        private List<string> childlessCommitHashes = new List<string>();
        private GitRevision rootCommit = null;

        public event EventHandler<CommitsChangedEventArgs> CommitsChanged;

        public GitRevision[] Commits
        {
            get
            {
                return this.commitsByHash.Values.ToArray();
            }
            set
            {
                bool isDoingInitialLoad = false;
                if (commitsByHash.Count == 0)
                {
                    isDoingInitialLoad = true;
                }

                var addedRevisions = GetAddedRevisions(Commits, value);
                var removedRevisions = GetRemovedRevisions(Commits, value);

                commitsByHash.Clear();
                commitHashes = new string[value.Length];
                int i = 0;
                foreach (var commit in value)
                {
                    commitsByHash.Add(commit.Hash, commit);
                    commitHashes[i] = commit.Hash;
                    i++;
                }
                FindRootCommit();
                FindChildlessCommits();

                var oldCellNumbersByHash = new Dictionary<string, Point>(cellNumberByHash);

                cellNumberByHash.Clear();
                listOfUsedRowsByColumn.Clear();
                foreach (var childlessHash in childlessCommitHashes)
                {
                    int nextRow = 0;
                    foreach (var location in cellNumberByHash.Values)
                    {
                        nextRow = Math.Max(nextRow, location.Y);
                    }
                    nextRow++;
                    CalculateCommitLocations(childlessHash, nextRow);
                }

                string preReversalRightmostCommitHash = GetRightmostCommitHash();
                int maxX = cellNumberByHash[preReversalRightmostCommitHash].X;

                foreach (var hash in cellNumberByHash.Keys.ToArray())
                {
                    Point preReversal = cellNumberByHash[hash];
                    cellNumberByHash[hash] = new Point(maxX - preReversal.X + 1, preReversal.Y);
                }

                var movedRevisions = GetMovedRevisions(oldCellNumbersByHash, cellNumberByHash);

                if (addedRevisions.Count > 0 || removedRevisions.Count > 0)
                {
                    CommitsChanged?.Invoke(this, new CommitsChangedEventArgs(addedRevisions.ToArray(), removedRevisions.ToArray(), movedRevisions.ToArray(), isDoingInitialLoad));
                }
            }
        }

        private List<MovedGitRevision> GetMovedRevisions(Dictionary<string, Point> oldCellNumbersByHash, Dictionary<string, Point> cellNumberByHash)
        {
            List<MovedGitRevision> movedRevisions = new List<MovedGitRevision>();

            foreach (var oldCellByHash in oldCellNumbersByHash)
            {
                // If the key doesn't exist anymore, it was removed, not moved, so skip it.
                if (cellNumberByHash.ContainsKey(oldCellByHash.Key))
                {
                    Point oldCell = oldCellByHash.Value;
                    Point newCell = cellNumberByHash[oldCellByHash.Key];

                    if  (!oldCell.Equals(newCell))
                    {
                        MovedGitRevision movedRevision = new MovedGitRevision(commitsByHash[oldCellByHash.Key], oldCell, newCell);
                        movedRevisions.Add(movedRevision);
                    }
                }
            }

            return movedRevisions;
        }

        private List<GitRevision> GetAddedRevisions(GitRevision[] revisionsBefore, GitRevision[] revisionsAfter)
        {
            List<GitRevision> addedRevisions = new List<GitRevision>();
            foreach (var after in revisionsAfter)
            {
                bool found = false;
                foreach (var before in revisionsBefore)
                {
                    if (after.Hash == before.Hash)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    addedRevisions.Add(after);
                }
            }

            return addedRevisions;
        }

        private List<GitRevision> GetRemovedRevisions(GitRevision[] revisionsBefore, GitRevision[] revisionsAfter)
        {
            List<GitRevision> removedRevisions = new List<GitRevision>();
            foreach (var before in revisionsBefore)
            {
                bool found = false;
                foreach (var after in revisionsAfter)
                {
                    if (after.Hash == before.Hash)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    removedRevisions.Add(before);
                }
            }
            return removedRevisions;
        }

        public GitReference[] Branches { get; set; }

        public bool HasPlottedCommit(string commitHash)
        {
            return cellNumberByHash.ContainsKey(commitHash);
        }

        public Point GetCellNumber(string commitHash)
        {
            if (!HasPlottedCommit(commitHash))
            {
                throw new Exception("Commit hash not found: " + commitHash);
            }
            return cellNumberByHash[commitHash];
        }

        public string GetLeftmostCommitHash()
        {
            int minX = int.MaxValue;
            string leftmostCommitHash = null;
            foreach (GitRevision commit in Commits)
            {
                if (!HasPlottedCommit(commit.Hash))
                {
                    continue;
                }

                int currentX = GetCellNumber(commit.Hash).X;
                if (currentX < minX)
                {
                    minX = currentX;
                    leftmostCommitHash = commit.Hash;
                }
            }

            return leftmostCommitHash;
        }

        public string GetRightmostCommitHash()
        {
            int maxX = int.MinValue;
            string rightmostCommitHash = null;
            foreach (GitRevision commit in Commits)
            {
                if (!HasPlottedCommit(commit.Hash))
                {
                    continue;
                }

                int currentX = GetCellNumber(commit.Hash).X;
                if (currentX > maxX)
                {
                    maxX = currentX;
                    rightmostCommitHash = commit.Hash;
                }
            }

            return rightmostCommitHash;
        }



        private Dictionary<string, List<string>> GetChildrenOfCommitsKeyedByHash()
        {
            Dictionary<string, List<string>> childrenOfCommitsByHash = new Dictionary<string, List<string>>();

            foreach (var commit in commitsByHash)
            {
                childrenOfCommitsByHash.Add(commit.Value.Hash, new List<string>());
            }
            foreach (var commit in commitsByHash)
            {
                foreach (var parentCommitHash in commit.Value.ParentHashes)
                {
                    childrenOfCommitsByHash[parentCommitHash].Add(commit.Value.Hash);
                }
            }

            return childrenOfCommitsByHash;
        }

        private void FindChildlessCommits()
        {
            childlessCommitHashes.Clear();

            Dictionary<string, List<string>> childrenOfCommitsByHash = GetChildrenOfCommitsKeyedByHash();

            foreach (var childrenOfCommitByHash in childrenOfCommitsByHash)
            {
                if (childrenOfCommitByHash.Value.Count == 0)
                {
                    childlessCommitHashes.Add(childrenOfCommitByHash.Key);
                }
            }
        }

        void CalculateCommitLocations(string mostRecentCommitHash, int offset)
        {
            string[] rowOwnerHashByRow = new string[10000];

            Dictionary<string, List<string>> childrenOfCommitsByHash = GetChildrenOfCommitsKeyedByHash();
            var reservationCommitHashByRowNumber = new List<string>();

            // Each commit should already have a column reserved for it.  Column reservations for parents are made when the child is drawn.

            // Reserve a spot for the first (most recent) commit.
            reservationCommitHashByRowNumber.Add(mostRecentCommitHash);

            bool foundStartCommit = false;
            int x = 0;
            for (int commitIndex = 0; commitIndex < commitHashes.Length; commitIndex++)
            {
                var commitHash = commitHashes[commitIndex];
                x++;
                if (cellNumberByHash.ContainsKey(commitHash))
                {
                    continue;
                }
                if (commitHash == mostRecentCommitHash)
                {
                    foundStartCommit = true;
                }
                if (!foundStartCommit)
                {
                    continue;
                }
                var commit = commitsByHash[commitHash];

                int row = reservationCommitHashByRowNumber.IndexOf(commit.Hash);
                if (row == -1)
                {
                    // This commit isn't an ancestor of our starting point.  Probably from another branch.
                    continue;
                }
                cellNumberByHash.Add(commit.Hash, new Point(x, row + offset));

                var newReservationCommitHashByRowNumber = new List<string>(reservationCommitHashByRowNumber);

                // Reserve rows for the parents.

                if (commit.ParentHashes.Count == 0)
                {
                    // The commit has no parent.  No reservations to make.
                    newReservationCommitHashByRowNumber.RemoveAt(row);
                }
                else
                {
                    for (int i = 1; i < commit.ParentHashes.Count; i++)
                    {
                        if (!newReservationCommitHashByRowNumber.Contains(commit.ParentHashes[i]))
                        {
                            newReservationCommitHashByRowNumber.Insert(row + 1, commit.ParentHashes[i]);
                        }
                    }
                    // Parent 0 replaces this child in the reservations.
                    newReservationCommitHashByRowNumber.RemoveAt(row);
                    if (!newReservationCommitHashByRowNumber.Contains(commit.ParentHashes[0]))
                    {
                        newReservationCommitHashByRowNumber.Insert(row, commit.ParentHashes[0]);
                    }
                }

                reservationCommitHashByRowNumber = newReservationCommitHashByRowNumber;
            }
        }

        private void FindRootCommit()
        {
            if (commitsByHash.Count == 0)
            {
                rootCommit = null;
                return;
            }
            // Pick an arbitrary commit and follow its parents up to the first commit.
            GitRevision workingCommit = commitsByHash.First().Value;
            while (true)
            {
                if (workingCommit.ParentHashes.Count == 0)
                {
                    break;
                }
                else
                {
                    string firstParentCommitHash = workingCommit.ParentHashes[0];
                    workingCommit = commitsByHash[firstParentCommitHash];
                }
            }
            rootCommit = workingCommit;
        }

        private Point getCommitCellNumber(GitRevision commit)
        {
            return cellNumberByHash[commit.Hash];
        }
    }
}
