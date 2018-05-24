using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace GitViewer
{
    class CommitToolTipRenderer
    {
        const int width = 300;
        public Font Font { get; set; }

        public int Paint(Graphics graphics, GitRevision revision)
        {
            graphics.Clear(Color.Black);
            Brush descriptionBrush = Brushes.Cyan;
            Brush authorBrush = Brushes.White;
            Brush dateBrush = Brushes.White;
            Brush contextDiffBrush = Brushes.White;
            Brush addedDiffBrush = Brushes.SpringGreen;
            Brush removedDiffBrush = Brushes.PaleVioletRed;

            string[] commitLines = revision.Diff.Diff.Split('\n');

            int y = 0;
            DrawStringAndAdvanceY(graphics, ref y, authorBrush, "Author: " + revision.Author.Name);
            DrawStringAndAdvanceY(graphics, ref y, dateBrush, "Date: " + revision.CommitterDate);
            DrawStringAndAdvanceY(graphics, ref y, descriptionBrush, " ");
            DrawStringAndAdvanceY(graphics, ref y, descriptionBrush, "    " + revision.Description);
            DrawStringAndAdvanceY(graphics, ref y, descriptionBrush, " ");

            for (int lineNumber = 0; lineNumber < commitLines.Length; lineNumber++)
            {
                Brush brush = contextDiffBrush;

                const int maxDiffLines = 30;
                if (lineNumber == maxDiffLines)
                {
                    DrawStringAndAdvanceY(graphics, ref y, brush, "[+" + (commitLines.Length - lineNumber) + " more lines]");
                    break;
                }
                var commitLine = commitLines[lineNumber];
                if (commitLine.Length == 0)
                {
                    continue;
                }
                if (commitLine[0] == '+')
                {
                    brush = addedDiffBrush;
                }
                else if (commitLine[0] == '-')
                {
                    brush = removedDiffBrush;
                }
                DrawStringAndAdvanceY(graphics, ref y, brush, commitLine);
            }

            return y;
        }

        public Size GetNeededSize(GitRevision revision)
        {
            // Create a temporary Graphics to call Paint() with.  Paint() returns the needed height.

            using (var discardableBitmap = new Bitmap(width, 1))
            {
                using (Graphics graphics = Graphics.FromImage(discardableBitmap))
                {
                    int height = Paint(graphics, revision);
                    return new Size(300, height);
                }
            }
        }

        private void DrawStringAndAdvanceY(Graphics graphics, ref int y, Brush brush, string text)
        {
            SizeF size = graphics.MeasureString(text, Font, (int)graphics.VisibleClipBounds.Width);
            graphics.DrawString(text, Font, brush, new RectangleF(new PointF(0, y), new SizeF(width, 10000)));

            y += (int)Math.Ceiling(size.Height);
        }
    }
}
