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

        FileSystemWatcher fileSystemWatcher;
        System.Windows.Forms.Timer fileSystemModificationWaitTimer;

        public ViewerForm()
        {
            InitializeComponent();
            this.KeyPreview = true;
            Text = Program.AppName;

            random = new Random((int)DateTime.Now.Ticks);

            LoadRepositoryDirectoryFromOptions();

            this.git = new Git(@"git.exe", gitRepo);

            fileSystemWatcher = new FileSystemWatcher(gitRepo, "*.*");
            fileSystemWatcher.IncludeSubdirectories = true;
            fileSystemWatcher.Changed += FileSystemWatcher_Changed;
            fileSystemWatcher.Created += FileSystemWatcher_Changed;
            fileSystemWatcher.Deleted += FileSystemWatcher_Changed;
            fileSystemWatcher.Renamed += FileSystemWatcher_Changed;
            fileSystemWatcher.EnableRaisingEvents = true;

            fileSystemModificationWaitTimer = new System.Windows.Forms.Timer();
            fileSystemModificationWaitTimer.Interval = 100;
            fileSystemModificationWaitTimer.Tick += FileSystemModificationsWaitTimer_Tick;

            PopulateGraph();
        }

        private void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
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

            if (DateTime.Now.Subtract(lastFileSystemChange).TotalMilliseconds > 700)
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

            plotter.Branches = branches.ToArray();

            graphViewer.Plotter = plotter;
            plotter.Commits = commits.ToArray();
            graphViewer.Branches = branches.ToArray();
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
            if (AskForRepositoryDirectory())
            {
                if (gitRepo != oldGitRepo)
                {
                    this.git = new Git(@"git.exe", gitRepo);
                    fileSystemWatcher.Path = gitRepo;
                    PopulateGraph();
                }
            }
        }

        private bool AskForRepositoryDirectory()
        {
            var options = new Options();
            options.Load();

            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Choose git repository directory";
                dialog.RootFolder = Environment.SpecialFolder.DesktopDirectory;
                if (options.RepositoryDirectory != null)
                {
                    dialog.SelectedPath = options.RepositoryDirectory;
                }
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    options.RepositoryDirectory = dialog.SelectedPath;
                    options.Save();

                    gitRepo = options.RepositoryDirectory;

                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private void LoadRepositoryDirectoryFromOptions()
        {
            Options options = new Options();
            options.Load();

            if (options.RepositoryDirectory == null || !Directory.Exists(options.RepositoryDirectory))
            {
                if (!AskForRepositoryDirectory())
                {
                    System.Environment.Exit(1);
                }
                // That should have populated gitRepo with the new directory.
            }
            else
            {
                gitRepo = options.RepositoryDirectory;
            }
        }
    }
}
