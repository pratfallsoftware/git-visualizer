using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace GitViewer
{
    public class GitDiff
    {
        public string Diff { get; }

        public string HashOfDiff
        {
            get
            {
                return GetOrCalculateDiffHash();
            }
        }

        private string hashOfDiff = null;

        public GitDiff(string diff)
        {
            this.Diff = diff;
        }

        private string GetOrCalculateDiffHash()
        {
            if (hashOfDiff != null)
            {
                return hashOfDiff;
            }

            hashOfDiff = HashString(Diff);
            return hashOfDiff;
        }

        private string HashString(string str)
        {
            SHA256Managed sha = new SHA256Managed();
            byte[] diffHashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(str));
            string diffHashString = "";
            foreach (byte diffHashByte in diffHashBytes)
            {
                diffHashString += String.Format("{0:x2}", diffHashByte);
            }

            return diffHashString;
        }
    }
}
