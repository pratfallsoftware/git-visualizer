using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitViewer
{
    public class CheckoutRequestedEventArgs : EventArgs
    {
        public string EntityToCheckOut { get; }

        public CheckoutRequestedEventArgs(string entityToCheckOut)
        {
            EntityToCheckOut = entityToCheckOut;
        }
    }
}
