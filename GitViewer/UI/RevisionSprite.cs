using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jdenticon;

namespace GitViewer
{
    class RevisionSprite
    {
        public GitRevision Revision { get; }

        public AnimatedValue<int> X { get; }
        public AnimatedValue<int> Y { get; }
        public AnimatedValue<float> Opacity { get; }

        public RevisionSprite(GitRevision revision)
        {
            Revision = revision;

            X = new AnimatedValue<int>(0);
            Y = new AnimatedValue<int>(0);
            Opacity = new AnimatedValue<float>(1);
        }

        public RevisionSprite(GitRevision revision, int x, int y)
            : this(revision, x, y, 1)
        {
        }

        public RevisionSprite(GitRevision revision, int x, int y, float opacity)
        {
            Revision = revision;
            X = new AnimatedValue<int>(x);
            Y = new AnimatedValue<int>(y);
            Opacity = new AnimatedValue<float>(opacity);
        }

        public Bitmap GetIdenticon(int size)
        {
            Color fillColor;

            byte r = byte.Parse(Revision.Diff.HashOfDiff.Substring(0, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
            byte g = byte.Parse(Revision.Diff.HashOfDiff.Substring(2, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
            byte b = byte.Parse(Revision.Diff.HashOfDiff.Substring(4, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
            fillColor = Color.FromArgb(r, g, b);

            var identicon = Identicon.FromValue(Revision.Diff.HashOfDiff, size);
            identicon.Style = new IdenticonStyle();
            identicon.Style.BackColor = Jdenticon.Rendering.Color.FromArgb(255, fillColor.R, fillColor.G, fillColor.B);

            return identicon.ToBitmap();
        }
    }
}
