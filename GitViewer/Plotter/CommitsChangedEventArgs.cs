using System;

namespace GitViewer
{
    public class CommitsChangedEventArgs : EventArgs
    {
        public GitRevision[] AddedCommits { get; }
        public GitRevision[] RemovedCommits { get; }
        public MovedGitRevision[] MovedCommits { get; }
        /// <summary>
        /// True if the plotter had no commits prior to this change.  This allows for different behavior (e.g. not fading in) when
        /// first loading everything vs when things change.
        /// </summary>
        public bool WasTriggeredByInitialLoad { get; }

        public CommitsChangedEventArgs(GitRevision[] addedCommits, GitRevision[] removedCommits, MovedGitRevision[] movedCommits, bool wasTriggeredByInitialLoad)
        {
            this.AddedCommits = addedCommits;
            this.RemovedCommits = removedCommits;
            this.MovedCommits = movedCommits;
            this.WasTriggeredByInitialLoad = wasTriggeredByInitialLoad;
        }
    }
}