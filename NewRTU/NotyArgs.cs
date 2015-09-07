using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NewRTU
{
    public class NotyArgs : EventArgs
    {
        public string Message { get; set; }
        public NotyArgs(string message)
        {
            Message = message;
        }

    }
}
