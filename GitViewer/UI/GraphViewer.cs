using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace GitViewer
{
    public partial class GraphViewer : Control
    {
        private const int cellWidth = 80;
        private const int cellHeight = 60;
        private const int circleSize = 30;

        private Font fontForGitRefs;
        private Font fontForWatermark;
        private ToolTip toolTip;
        private CommitToolTipRenderer commitToolTipRenderer = new CommitToolTipRenderer();
        private Timer animationTimer = null;
        private ContextMenu revisionContextMenu = null;
        private RevisionSprite spriteContextMenuAppliesTo = null;
        private Random random = new Random();

        private Dictionary<string, List<GitReference>> listOfRefsByCommitHash = new Dictionary<string, List<GitReference>>();
        private Dictionary<string, RevisionSprite> spritesByCommitHash = new Dictionary<string, RevisionSprite>();

        private RevisionSprite hoveredOverSprite = null;

        DateTime animationEndTime = DateTime.Now;

        public string WatermarkText { get; set; }

        public event EventHandler<CheckoutRequestedEventArgs> CheckoutRequested;

        private CommitGraphPlotter plotter = null;
        public CommitGraphPlotter Plotter
        {
            get
            {
                return this.plotter;
            }
            set
            {
                if (this.plotter != value)
                {
                    if (this.plotter != null)
                    {
                        this.plotter.CommitsChanged -= Plotter_CommitsChanged;
                    }
                    this.plotter = value;

                    spritesByCommitHash.Clear();

                    if (this.plotter != null)
                    {
                        this.plotter.CommitsChanged += Plotter_CommitsChanged;

                        if (plotter.Commits != null)
                        {
                            foreach (var revision in plotter.Commits)
                            {
                                Point cellNumber = Plotter.GetCellNumber(revision.Hash);
                                Point location = CellNumberToAbsoluteLocation(cellNumber);
                                var sprite = new RevisionSprite(revision, location.X, location.Y, 1);
                                spritesByCommitHash.Add(revision.Hash, sprite);
                            }
                        }
                    }
                }
            }
        }

        Point originOffset = Point.Empty;
        public Point OriginOffset
        {
            get
            {
                return originOffset;
            }
            set
            {
                originOffset = value;
                Invalidate();
            }
        }

        private GitReference[] branches = new GitReference[0];
        public GitReference[] Branches
        {
            get
            {
                return this.branches;
            }
            set
            {
                this.branches = value;
                this.listOfRefsByCommitHash.Clear();
                foreach (GitReference branch in branches)
                {
                    if (!this.listOfRefsByCommitHash.ContainsKey(branch.CommitHash))
                    {
                        this.listOfRefsByCommitHash.Add(branch.CommitHash, new List<GitReference>());
                    }
                    this.listOfRefsByCommitHash[branch.CommitHash].Add(branch);
                }
                Invalidate();
            }
        }

        public string CurrentBranch { get; set; }

        public GraphViewer()
        {
            InitializeComponent();

            DoubleBuffered = true;
            SetStyle(ControlStyles.Selectable, true);

            BackColor = Color.White;
            Font = new Font(this.Font.FontFamily, 10);

            fontForGitRefs = new Font(this.Font, FontStyle.Bold);
            fontForWatermark = new Font(this.Font.FontFamily, this.Font.SizeInPoints * 3, FontStyle.Bold);

            ConfigureTooltipDisplayOptions();

            var checkoutMenuItem = new MenuItem("Check out");
            checkoutMenuItem.Click += CheckoutMenuItem_Click;
            revisionContextMenu = new ContextMenu(new MenuItem[] { checkoutMenuItem });
        }

        protected override void OnCreateControl()
        {
            // DesignMode is not set until after the constructor finishes
            if (!DesignMode)
            {
                // Background pattern from Toptal Subtle Patterns
                BackgroundImage = Image.FromFile(@"Resources\groovepaper.png");
            }

            base.OnCreateControl();
        }

        private void ConfigureTooltipDisplayOptions()
        {
            toolTip = new ToolTip();
            toolTip.AutoPopDelay = 30000;
            toolTip.InitialDelay = 100;
            toolTip.ReshowDelay = 500;
            // Force the ToolTip text to be displayed whether or not the form is active.
            toolTip.ShowAlways = true;
            toolTip.OwnerDraw = true;
            toolTip.Popup += ToolTip_Popup;
            toolTip.Draw += ToolTip_Draw;
        }

        private void ToolTip_Popup(object sender, PopupEventArgs e)
        {
            commitToolTipRenderer.Font = Font;
            e.ToolTipSize = commitToolTipRenderer.GetNeededSize(hoveredOverSprite.Revision);
        }

        private void ToolTip_Draw(object sender, DrawToolTipEventArgs e)
        {
            List<GitReference> references = GetReferencesForRevision(hoveredOverSprite.Revision.Hash);
            string[] referenceNames = new string[references.Count];
            for (int i = 0; i < references.Count; i++)
            {
                referenceNames[i] = references[i].ShortName;
            }

            commitToolTipRenderer.Font = Font;
            commitToolTipRenderer.Paint(e.Graphics, hoveredOverSprite.Revision);
        }

        protected override void OnClick(EventArgs e)
        {
            // Clicking the control does not focus it automatically.
            this.Focus();
            base.OnClick(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (e.Button == MouseButtons.Right)
            {
                if (hoveredOverSprite != null)
                {
                    spriteContextMenuAppliesTo = hoveredOverSprite;
                    revisionContextMenu.Show(this, e.Location);
                }
            }
        }

        private void CheckoutMenuItem_Click(object sender, EventArgs e)
        {
            string selectedRevisionHash = spriteContextMenuAppliesTo.Revision.Hash;

            // There may be multiple branches pointed at this commit.
            var candidateReferences = new List<GitReference>();
            foreach (var reference in Plotter.Branches)
            {
                if (reference.CommitHash == selectedRevisionHash)
                {
                    candidateReferences.Add(reference);
                }
            }

            string entityToCheckOut = null;

            // Prefer local branches
            foreach (var reference in candidateReferences)
            {
                if (reference.Type == GitReferenceType.Head)
                {
                    entityToCheckOut = reference.ShortName;
                    break;
                }
            }

            // If no local branches apply, try the remotes
            if (entityToCheckOut == null)
            {
                foreach (var reference in candidateReferences)
                {
                    if (reference.Type == GitReferenceType.Remote && reference.ShortName != "HEAD")
                    {
                        string nameWithoutPrefix = reference.FullName.Substring("refs/".Length);
                        entityToCheckOut = nameWithoutPrefix;
                        break;
                    }
                }
            }

            // Last resort -- detached head
            if (entityToCheckOut == null)
            {
                entityToCheckOut = selectedRevisionHash;
            }

            CheckoutRequested?.Invoke(this, new CheckoutRequestedEventArgs(entityToCheckOut));
        }

        private void Plotter_CommitsChanged(object sender, CommitsChangedEventArgs e)
        {
            const int moveAnimationDurationInMilliseconds = 2500;
            const int fadeInDurationInMilliseconds = 3000;

            Console.WriteLine("Added " + e.AddedCommits.Length + ", removed " + e.RemovedCommits.Length + ", moved " + e.MovedCommits.Length);

            foreach (var commit in e.AddedCommits)
            {
                spritesByCommitHash.Add(commit.Hash, new RevisionSprite(commit));

                Point destinationLocation = CellNumberToAbsoluteLocation(Plotter.GetCellNumber(commit.Hash));

                // When we first start up, we don't want to fade in everything.  But when commits get added, we want to animate that.
                if (e.WasTriggeredByInitialLoad)
                {
                    spritesByCommitHash[commit.Hash].X.PopTo(destinationLocation.X);
                    spritesByCommitHash[commit.Hash].Y.PopTo(destinationLocation.Y);
                    spritesByCommitHash[commit.Hash].Opacity.PopTo(1);
                }
                else
                {
                    // If this commit is being copied from another commit (i.e. if the hash is identical to another commit), animate moving it from the old commit to the new one.
                    List<string> duplicateDiffCommitHashes = this.GetCommitsWithIdenticalDiff(commit.Hash);
                    if (duplicateDiffCommitHashes.Count > 0)
                    {
                        // Arbitrarily pick one of the commits to say it's a duplicate of if there's more than one.
                        var duplicatedRevision = duplicateDiffCommitHashes[duplicateDiffCommitHashes.Count - 1];

                        Point sourceLocation = CellNumberToAbsoluteLocation(Plotter.GetCellNumber(duplicatedRevision));

                        // The source cell might be moving (done down below), so we need the start location, not the end location.
                        foreach (var movedCommit in e.MovedCommits)
                        {
                            if (movedCommit.Revision.Hash == duplicatedRevision)
                            {
                                sourceLocation = CellNumberToAbsoluteLocation(movedCommit.StartCellCoordinates);
                                break;
                            }
                        }

                        spritesByCommitHash[commit.Hash].X.Animate(sourceLocation.X, destinationLocation.X, DateTime.Now, DateTime.Now.AddMilliseconds(moveAnimationDurationInMilliseconds));
                        spritesByCommitHash[commit.Hash].Y.Animate(sourceLocation.Y, destinationLocation.Y, DateTime.Now, DateTime.Now.AddMilliseconds(moveAnimationDurationInMilliseconds));
                        spritesByCommitHash[commit.Hash].Opacity.PopTo(1);
                    }
                    else
                    {
                        spritesByCommitHash[commit.Hash].X.PopTo(destinationLocation.X);
                        spritesByCommitHash[commit.Hash].Y.PopTo(destinationLocation.Y);
                        spritesByCommitHash[commit.Hash].Opacity.Animate(0, 1, DateTime.Now, DateTime.Now.AddMilliseconds(fadeInDurationInMilliseconds));
                    }
                }
            }
                
            foreach (var commit in e.MovedCommits)
            {
                Point sourceLocation = CellNumberToAbsoluteLocation(commit.StartCellCoordinates);
                Point destinationLocation = CellNumberToAbsoluteLocation(commit.EndCellCoordinates);
                spritesByCommitHash[commit.Revision.Hash].X.Animate(sourceLocation.X, destinationLocation.X, DateTime.Now, DateTime.Now.AddMilliseconds(moveAnimationDurationInMilliseconds));
                spritesByCommitHash[commit.Revision.Hash].Y.Animate(sourceLocation.Y, destinationLocation.Y, DateTime.Now, DateTime.Now.AddMilliseconds(moveAnimationDurationInMilliseconds));
            }

            foreach (var commit in e.RemovedCommits)
            {
                spritesByCommitHash.Remove(commit.Hash);
            }

            StartAnimationTimer(3);
            Invalidate();
        }

        private void StartAnimationTimer(int durationInSeconds)
        {
            if (animationTimer == null)
            {
                animationTimer = new Timer();
                animationTimer.Tick += AnimationTimer_Tick;
            }
            animationTimer.Stop();
            animationTimer.Interval = 10;
            animationEndTime = DateTime.Now.AddSeconds(durationInSeconds);
            animationTimer.Start();
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            if (DateTime.Now > animationEndTime)
            {
                animationTimer.Stop();
            }
            Invalidate();
        }

        private Point GetSpriteDrawingCoordinates(RevisionSprite sprite, Point originOffset)
        {
            return new Point(
                sprite.X.Value - originOffset.X,
                sprite.Y.Value - originOffset.Y
            );
        }

        private Point GetSpriteDrawingCoordinates(RevisionSprite sprite)
        {
            return GetSpriteDrawingCoordinates(sprite, OriginOffset);
        }

        private Point CellNumberToAbsoluteLocation(Point cellNumber)
        {
            return new Point(cellNumber.X * cellWidth, cellNumber.Y * cellHeight);
        }

        private Point CellNumberToDrawingCoordinates(Point cellNumber, Point originOffset)
        {
            return new Point(cellNumber.X * cellWidth - originOffset.X, cellNumber.Y * cellHeight - originOffset.Y);
        }

        public void CenterOnCommit(string commitHash)
        {
            foreach (var sprite in spritesByCommitHash.Values)
            {
                if (sprite.Revision.Hash == commitHash)
                {
                    Point location = GetSpriteDrawingCoordinates(sprite, new Point(0, 0));
                    OriginOffset = new Point(location.X - Width / 2, location.Y - Height / 2);
                    return;
                }
            }
            throw new ArgumentException("Commit hash not available: " + commitHash);
        }

        private RevisionSprite GetSpriteAtDrawingCoordinates(Point drawingCoordinates)
        {
            foreach (var sprite in spritesByCommitHash.Values)
            {
                Point center = GetSpriteDrawingCoordinates(sprite);
                double distance = Math.Sqrt(Math.Pow(drawingCoordinates.X - center.X, 2) + Math.Pow(drawingCoordinates.Y - center.Y, 2));

                if (distance < (double)circleSize / 2.0)
                {
                    return sprite;
                }
            }
            return null;
        }

        private List<GitReference> GetReferencesForRevision(string revisionHash)
        {
            List<GitReference> references;
            if (listOfRefsByCommitHash.ContainsKey(revisionHash))
            {
                references = listOfRefsByCommitHash[revisionHash];
            }
            else
            {
                references = new List<GitReference>();
            }
            return references;
        }

        public void UpdateGraph()
        {
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);

            if (DesignMode)
            {
                return;
            }

            pe.Graphics.SmoothingMode = SmoothingMode.HighQuality;

            DrawBackground(pe.Graphics);

            foreach (var sprite in spritesByCommitHash.Values)
            {
                var commit = sprite.Revision;
                if (!Plotter.HasPlottedCommit(commit.Hash))
                {
                    continue;
                }
                Point location = GetSpriteDrawingCoordinates(sprite);

                Rectangle commitBounds = new Rectangle(new Point(location.X - cellWidth / 2, location.Y - cellHeight / 2), new Size(cellWidth, cellHeight));
                if (pe.ClipRectangle.IntersectsWith(commitBounds))
                {
                    Point circleUpperLeft = new Point(location.X - circleSize / 2, location.Y - circleSize / 2);

                    Rectangle circleBounds = new Rectangle(circleUpperLeft, new Size(circleSize, circleSize));

                    DrawCommitCircle(pe.Graphics, sprite, circleBounds);

                    int lineSpacing = 4;
                    int yPosition = location.Y + circleSize / 2 + lineSpacing;

                    string shortCommitHash = commit.Hash.Substring(0, 8);
                    SizeF stringSize = pe.Graphics.MeasureString(shortCommitHash, this.Font);
                    pe.Graphics.DrawString(shortCommitHash, this.Font, Brushes.Black, new Point(location.X - (int)stringSize.Width / 2, yPosition));
                    yPosition += (int)Math.Ceiling(stringSize.Height);

                    const int labelMaxLength = 13;
                    if (listOfRefsByCommitHash.ContainsKey(commit.Hash))
                    {
                        foreach (GitReference reference in listOfRefsByCommitHash[commit.Hash])
                        {
                            string branchName = reference.ShortName;
                            if (branchName.Length > labelMaxLength)
                            {
                                branchName = "..." + branchName.Substring(branchName.Length - labelMaxLength, labelMaxLength);
                            }

                            Brush refBrush;
                            switch (reference.Type)
                            {
                                case GitReferenceType.Head:
                                    refBrush = Brushes.Blue;
                                    break;
                                case GitReferenceType.Remote:
                                    refBrush = Brushes.Maroon;
                                    break;
                                case GitReferenceType.Stash:
                                    refBrush = Brushes.Purple;
                                    break;
                                case GitReferenceType.Tag:
                                    refBrush = Brushes.DarkGreen;
                                    break;
                                default:
                                    refBrush = Brushes.Black;
                                    break;
                            }
                            stringSize = pe.Graphics.MeasureString(branchName, fontForGitRefs);
                            var style = FontStyle.Regular;
                            if (CurrentBranch == reference.ShortName && reference.Type == GitReferenceType.Head)
                            {
                                style = FontStyle.Underline;
                            }
                            using (Font font = new Font(fontForGitRefs, style))
                            {
                                pe.Graphics.DrawString(branchName, font, refBrush, new Point(location.X - (int)stringSize.Width / 2, yPosition));
                            }
                            yPosition += (int)Math.Ceiling(stringSize.Height);
                        }
                    }
                }
            }

            DrawArrowsFromChildToParentCommits(pe.Graphics);
        }

        private void DrawArrowsFromChildToParentCommits(Graphics graphics)
        {
            using (Pen arrowPen = new Pen(Color.Blue, 1))
            {
                AdjustableArrowCap arrowCap = new AdjustableArrowCap(4, 4);
                arrowPen.SetLineCap(System.Drawing.Drawing2D.LineCap.RoundAnchor, System.Drawing.Drawing2D.LineCap.Custom, System.Drawing.Drawing2D.DashCap.Flat);
                arrowPen.CustomEndCap = arrowCap;

                foreach (var sprite in spritesByCommitHash.Values)
                {
                    var commit = sprite.Revision;
                    foreach (string parentHash in commit.ParentHashes)
                    {
                        if (Plotter.HasPlottedCommit(parentHash) && Plotter.HasPlottedCommit(commit.Hash))
                        {
                            var parentSprite = spritesByCommitHash[parentHash];
                            float fadeInAlpha = Math.Min(parentSprite.Opacity.Value, spritesByCommitHash[commit.Hash].Opacity.Value);
                            arrowPen.Color = Color.FromArgb((int)(fadeInAlpha * 255), Color.Blue);

                            Point location = this.GetSpriteDrawingCoordinates(sprite);
                            Point parentLocation = this.GetSpriteDrawingCoordinates(parentSprite);
                            Point leftSideOfParent = new Point(parentLocation.X - circleSize / 2, parentLocation.Y);
                            Point rightSideOfChild = new Point(location.X + circleSize / 2, location.Y);
                            Point rightSideOfParent = new Point(parentLocation.X + circleSize / 2, parentLocation.Y);
                            Point leftSideOfChild = new Point(location.X - circleSize / 2, location.Y);

                            graphics.DrawLine(arrowPen, leftSideOfChild, rightSideOfParent);
                        }
                    }
                }
            }
        }

        private void DrawBackground(Graphics graphics)
        {
            graphics.Clear(BackColor);
            using (TextureBrush brush = new TextureBrush(BackgroundImage, WrapMode.Tile))
            {
                graphics.FillRectangle(brush, 0, 0, Width, Height);
            }

            using (var brush = new SolidBrush(Color.FromArgb(64, 0, 0, 0)))
            {
                SizeF watermarkSize = graphics.MeasureString(WatermarkText, fontForWatermark);
                graphics.DrawString(WatermarkText, fontForWatermark, brush, new Point((int)(Width - 10 - watermarkSize.Width), (int)(Height - 10 - watermarkSize.Height)));
            }
        }

        private void DrawCommitCircle(Graphics graphics, RevisionSprite commitSprite, Rectangle circleBounds)
        {
            var circlePath = new GraphicsPath();
            circlePath.AddEllipse(circleBounds);
            var oldClip = graphics.ClipBounds;
            graphics.SetClip(circlePath);

            using (Bitmap bitmap = spritesByCommitHash[commitSprite.Revision.Hash].GetIdenticon(circleSize))
            {
                Point drawPoint = circleBounds.Location;
                // It looks uncentered unless we shift it down and right a little
                drawPoint.Offset(1, 1);

                float bitmapAlpha = commitSprite.Opacity.Value;
                TransparentImage.DrawImageWithAlpha(graphics, bitmap, drawPoint, bitmapAlpha);
            }

            graphics.SetClip(oldClip);

            if (hoveredOverSprite != null && hoveredOverSprite.Revision.Hash == commitSprite.Revision.Hash)
            {
                Color color = Color.FromArgb(255, 128, 0, 0);
                using (Pen pen = new Pen(color, 4))
                {
                    graphics.DrawEllipse(pen, circleBounds);
                }
            }
            else
            {
                Color color = Color.FromArgb((int)(commitSprite.Opacity.Value * 255), 128, 0, 0);
                using (Pen pen = new Pen(color, 1))
                {
                    graphics.DrawEllipse(pen, circleBounds);
                }
            }
        }

        private List<string> GetCommitsWithIdenticalDiff(string commitHash)
        {
            List<string> commitHashesWithMatchingDiff = new List<string>();

            string diffHash = spritesByCommitHash[commitHash].Revision.Diff.HashOfDiff;
            foreach (var commit in Plotter.Commits)
            {
                string diffHashThatMightMatch = commit.Diff.HashOfDiff;
                if (diffHash == diffHashThatMightMatch && commit.Hash != commitHash)
                {
                    commitHashesWithMatchingDiff.Add(commit.Hash);
                }
            }

            return commitHashesWithMatchingDiff;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var sprite = GetSpriteAtDrawingCoordinates(e.Location);
            if (hoveredOverSprite != sprite)
            {
                hoveredOverSprite = sprite;
                if (sprite != null && sprite.Revision.Diff != null)
                {
                    string text = "arbitrary text replaced by OwnerDraw event";
                    toolTip.SetToolTip(this, text);
                }
                else
                {
                    toolTip.SetToolTip(this, null);
                }
                Invalidate();
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            int deltaX = 0;
            int deltaY = 0;
            int stepSize = 125;
            bool handled = false;

            if (keyData == Keys.Right)
            {
                deltaX++;
                handled = true;
            }
            if (keyData == Keys.Left)
            {
                deltaX--;
                handled = true;
            }
            if (keyData == Keys.Up)
            {
                deltaY--;
                handled = true;
            }
            if (keyData == Keys.Down)
            {
                deltaY++;
                handled = true;
            }

            if (handled)
            {
                deltaX *= stepSize;
                deltaY *= stepSize;

                this.OriginOffset = new Point(this.OriginOffset.X + deltaX, this.OriginOffset.Y + deltaY);

                return true;
            }

            if (keyData == Keys.Home)
            {
                string leftmostCommitHash = this.Plotter.GetLeftmostCommitHash();
                this.CenterOnCommit(leftmostCommitHash);
                return true;
            }

            if (keyData == Keys.End)
            {
                string rightmostCommitHash = this.Plotter.GetRightmostCommitHash();
                this.CenterOnCommit(rightmostCommitHash);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            // Windows doesn't repaint everything by default on resizes.
            Invalidate();
        }
    }
}
