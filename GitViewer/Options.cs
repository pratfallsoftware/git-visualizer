using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitViewer
{
    class Options
    {
        string filename = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + Path.DirectorySeparatorChar
            + Program.AppName + Path.DirectorySeparatorChar
            + "options.txt";

        public string RepositoryDirectory { get; set; }

        private string CreateFileContents()
        {
            // This will eventually be YAML or JSON or somesuch, but no need for that yet.
            string contents =
                "Version: 1\n" +
                "RepositoryDirectory: " + RepositoryDirectory;
            return contents;
        }

        public void Save()
        {
            string directoryName = Path.GetDirectoryName(filename);
            Directory.CreateDirectory(directoryName);
            using (var writer = new StreamWriter(filename))
            {
                writer.Write(CreateFileContents());
            }
        }

        private Dictionary<string, string> ParseFileContents(string fileContents)
        {
            Dictionary<string, string> optionsAndValues = new Dictionary<string, string>();
            var fileLines = fileContents.Split('\n');
            foreach (var line in fileLines)
            {
                // Split on the first colon.
                int colonPosition = line.IndexOf(':');
                if (colonPosition == -1)
                {
                    throw new InvalidDataException("Expected options file to contains key/value pairs separated by a colon.");
                }

                string optionName = line.Substring(0, colonPosition).Trim();
                string value = line.Substring(colonPosition + 1).Trim();

                optionsAndValues.Add(optionName, value);
            }
            return optionsAndValues;
        }

        public void Load()
        {
            if (!File.Exists(filename))
            {
                return;
            }
            using (var reader = new StreamReader(filename))
            {
                string fileContents = reader.ReadToEnd();
                Dictionary<string, string> optionsAndValues = ParseFileContents(fileContents);
                if (optionsAndValues.ContainsKey("RepositoryDirectory"))
                {
                    RepositoryDirectory = optionsAndValues["RepositoryDirectory"];
                }
            }
        }
    }
}
