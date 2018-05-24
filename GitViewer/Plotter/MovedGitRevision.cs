using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace GitViewer
{
    public class MovedGitRevision
    {
        public GitRevision Revision { get; }
        public Point StartCellCoordinates { get; }
        public Point EndCellCoordinates { get; }

        public MovedGitRevision(GitRevision revision, Point startCellCoordinates, Point endCellCoordinates)
        {
            this.Revision = revision;
            this.StartCellCoordinates = startCellCoordinates;
            this.EndCellCoordinates = endCellCoordinates;
        }
    }
}
