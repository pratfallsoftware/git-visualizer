using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GitViewer
{
    public partial class ViewerForm : Form
    {
        delegate void addDiffDelegate(string commitHash, string diff);
        Git git;
        CommitGraphPlotter plotter = new CommitGraphPlotter();
        string gitRepo = null;
        Random random;
        DateTime lastFileSystemChange = DateTime.MinValue;
        RepositoryDirectoryController repositoryDirectoryController = new RepositoryDirectoryController();

        FileSystemWatcher fileSystemWatcher;
        System.Windows.Forms.Timer fileSystemModificationWaitTimer;

        public ViewerForm()
        {
            InitializeComponent();
            this.KeyPreview = true;
            Text = Program.AppName;
            this.Icon = Icon.FromHandle(Resources.Resources.Pratfall.GetHicon());

            random = new Random((int)DateTime.Now.Ticks);

            fileSystemModificationWaitTimer = new System.Windows.Forms.Timer();
            fileSystemModificationWaitTimer.Interval = 50;
            fileSystemModificationWaitTimer.Tick += FileSystemModificationsWaitTimer_Tick;

            graphViewer.CheckoutRequested += GraphViewer_CheckoutRequested;
        }

        private void GraphViewer_CheckoutRequested(object sender, CheckoutRequestedEventArgs e)
        {
            git.CheckOut(e.EntityToCheckOut);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            gitRepo = repositoryDirectoryController.GetOrAskForRepositoryDirectory();
            if (gitRepo == null)
            {
                // They canceled selecting a repo.
                Environment.Exit(0);
            }

            this.git = new Git(@"git.exe", gitRepo);

            fileSystemWatcher = new FileSystemWatcher(gitRepo, "*.*");
            fileSystemWatcher.IncludeSubdirectories = true;
            fileSystemWatcher.Changed += FileSystemWatcher_Changed;
            fileSystemWatcher.Created += FileSystemWatcher_Changed;
            fileSystemWatcher.Deleted += FileSystemWatcher_Changed;
            fileSystemWatcher.Renamed += FileSystemWatcher_Changed;
            fileSystemWatcher.EnableRaisingEvents = true;

            PopulateGraph();
        }

        private void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            // Ignore changes to the lock file.  Doing a "git status" updates this file, and we do a git status when files change, so ignoring it prevents an infinite loop.
            // Since that file is being created and destroyed, we also need to ignore changes to the .git directory itself.
            if (Path.GetFileName(e.FullPath) == "index.lock"
                || e.Name == ".git")
            {
                return;
            }
            lastFileSystemChange = DateTime.Now;
            // Start checking a few times per second whether it's been enough time since the last filesystem change to assume things are done changing.
            StartFileSystemModificationsWaitTimer();
        }

        private void StartFileSystemModificationsWaitTimer()
        {
            if (InvokeRequired)
            {
                Invoke((MethodInvoker)delegate ()
                {
                    StartFileSystemModificationsWaitTimer();
                });
                return;
            }

            fileSystemModificationWaitTimer.Start();
        }

        private void FileSystemModificationsWaitTimer_Tick(object sender, EventArgs e)
        {
            // This function waits a short time before reloading the repo.  We often get several filesystem updates in quick succession, so this prevents us from doing a dozen reloads all at once.

            if (DateTime.Now.Subtract(lastFileSystemChange).TotalMilliseconds > 400)
            {
                fileSystemModificationWaitTimer.Stop();
                Console.WriteLine(DateTime.Now + " Filesystem changed.  Refreshing.");
                PopulateGraph();
            }
        }

        private void PopulateGraph()
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

            for (int i = 0; i < branches.Count; i++)
            {
                // Don't include origin/HEAD -- it looks weird
                if (branches[i].FullName == "refs/remotes/origin/HEAD")
                {
                    branches.RemoveAt(i);
                    break;
                }
            }

            plotter.Branches = branches.ToArray();

            graphViewer.Plotter = plotter;
            plotter.Commits = commits.ToArray();
            graphViewer.Branches = branches.ToArray();
            graphViewer.CurrentBranch = currentBranch;
            graphViewer.WatermarkText = Path.GetFileName(gitRepo);

            graphViewer.UpdateGraph();
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
                Console.WriteLine("Refreshing...");
                PopulateGraph();
                e.Handled = true;
                fileSystemWatcher.EnableRaisingEvents = true;
            }
            base.OnKeyUp(e);
        }

        private void addRandomCommitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string filename = gitRepo + Path.DirectorySeparatorChar + "random_sample_file_" + random.Next(100, 100000) + ".txt";
            using (StreamWriter sw = new StreamWriter(filename, true))
            {
                sw.WriteLine("Appending random data: " + random.Next(1000, int.MaxValue));
            }
            git.RunGitCommand("add " + filename);
            git.RunGitCommand("commit -m \"Sample commit " + DateTime.Now.ToShortTimeString() + "\"");
        }

        private void changeRepoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ChangeRepositoryDirectory();
        }

        private void ChangeRepositoryDirectory()
        {
            string oldGitRepo = gitRepo;
            string directory = repositoryDirectoryController.AskForRepositoryDirectoryAndSaveIfValid();
            if (directory != null)
            {
                gitRepo = directory;
                if (gitRepo != oldGitRepo)
                {
                    this.git = new Git(@"git.exe", gitRepo);
                    fileSystemWatcher.Path = gitRepo;
                    PopulateGraph();
                }
            }
        }

    }
}
