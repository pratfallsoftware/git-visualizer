namespace GitViewer
{
    partial class ViewerForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.changeRepoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.gitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addRandomCommitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.graphViewer = new GitViewer.GraphViewer();
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showRemoteBranchesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.viewToolStripMenuItem,
            this.gitToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(761, 24);
            this.menuStrip1.TabIndex = 4;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.changeRepoToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // changeRepoToolStripMenuItem
            // 
            this.changeRepoToolStripMenuItem.Name = "changeRepoToolStripMenuItem";
            this.changeRepoToolStripMenuItem.Size = new System.Drawing.Size(145, 22);
            this.changeRepoToolStripMenuItem.Text = "Change &Repo";
            this.changeRepoToolStripMenuItem.Click += new System.EventHandler(this.changeRepoToolStripMenuItem_Click);
            // 
            // gitToolStripMenuItem
            // 
            this.gitToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addRandomCommitToolStripMenuItem});
            this.gitToolStripMenuItem.Name = "gitToolStripMenuItem";
            this.gitToolStripMenuItem.Size = new System.Drawing.Size(69, 20);
            this.gitToolStripMenuItem.Text = "Git &Demo";
            // 
            // addRandomCommitToolStripMenuItem
            // 
            this.addRandomCommitToolStripMenuItem.Name = "addRandomCommitToolStripMenuItem";
            this.addRandomCommitToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.R)));
            this.addRandomCommitToolStripMenuItem.Size = new System.Drawing.Size(227, 22);
            this.addRandomCommitToolStripMenuItem.Text = "Add random commit";
            this.addRandomCommitToolStripMenuItem.Click += new System.EventHandler(this.addRandomCommitToolStripMenuItem_Click);
            // 
            // graphViewer
            // 
            this.graphViewer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.graphViewer.BackColor = System.Drawing.Color.White;
            this.graphViewer.Branches = new GitViewer.GitReference[0];
            this.graphViewer.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.graphViewer.Location = new System.Drawing.Point(0, 27);
            this.graphViewer.Name = "graphViewer";
            this.graphViewer.OriginOffset = new System.Drawing.Point(0, 0);
            this.graphViewer.Plotter = null;
            this.graphViewer.Size = new System.Drawing.Size(761, 401);
            this.graphViewer.TabIndex = 0;
            this.graphViewer.Text = "graphViewer";
            this.graphViewer.WatermarkText = null;
            // 
            // viewToolStripMenuItem
            // 
            this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.showRemoteBranchesToolStripMenuItem});
            this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            this.viewToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.viewToolStripMenuItem.Text = "&View";
            // 
            // showRemoteBranchesToolStripMenuItem
            // 
            this.showRemoteBranchesToolStripMenuItem.CheckOnClick = true;
            this.showRemoteBranchesToolStripMenuItem.Name = "showRemoteBranchesToolStripMenuItem";
            this.showRemoteBranchesToolStripMenuItem.Size = new System.Drawing.Size(198, 22);
            this.showRemoteBranchesToolStripMenuItem.Text = "Show Remote Branches";
            this.showRemoteBranchesToolStripMenuItem.Click += new System.EventHandler(this.showRemoteBranchesToolStripMenuItem_Click);
            // 
            // ViewerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(761, 427);
            this.Controls.Add(this.graphViewer);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "ViewerForm";
            this.Text = "Visualizer For Git";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private GraphViewer graphViewer;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem gitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem addRandomCommitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem changeRepoToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showRemoteBranchesToolStripMenuItem;
    }
}

