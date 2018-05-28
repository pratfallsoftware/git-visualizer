using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GitViewer
{
    class RepositoryDirectoryController
    {
        Options options = null;

        public RepositoryDirectoryController()
        {
            options = new Options();
        }

        public string GetOrAskForRepositoryDirectory()
        {
            // This loop returns when they enter a valid directory or when they decline to give a directory.
            while (true)
            {
                options.Load();

                if (IsRepositoryDirectoryValid(options.RepositoryDirectory))
                {
                    return options.RepositoryDirectory;
                }

                string directory = AskForRepositoryDirectory();
                if (directory == null)
                {
                    // They canceled.  End the loop before saving the change.
                    return null;
                }
                options.RepositoryDirectory = directory;
                options.Save();
                return directory;
            }
        }

        private bool IsRepositoryDirectoryValid(string directory)
        {
            if (string.IsNullOrEmpty(directory))
            {
                return false;
            }

            if (!Directory.Exists(directory))
            {
                return false;
            }

            bool foundGitDirectory = false;
            List<string> directories = new List<string>(Directory.GetDirectories(directory));
            foreach (string subdirectory in directories)
            {
                if (Path.GetFileName(subdirectory) == ".git")
                {
                    foundGitDirectory = true;
                    break;
                }
            }
            if (!foundGitDirectory)
            {
                return false;
            }

            return true;
        }

        public string AskForRepositoryDirectory()
        {
            // Loop ends when they cancel or they choose a valid directory.
            while (true)
            {
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
                        if (IsRepositoryDirectoryValid(dialog.SelectedPath))
                        {
                            return dialog.SelectedPath;
                        }
                        else
                        {
                            MessageBox.Show("Directory is not a valid git project -- it does not contain a .git subdirectory.", "Can not use directory", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            // Loop again
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }

        public string AskForRepositoryDirectoryAndSaveIfValid()
        {
            string directory = AskForRepositoryDirectory();
            if (IsRepositoryDirectoryValid(directory))
            {
                options.Load();
                options.RepositoryDirectory = directory;
                options.Save();
                return directory;
            }

            return null;
        }
    }
}
