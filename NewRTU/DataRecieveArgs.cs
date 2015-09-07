using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NewRTU
{
    public class DataRecieveArgs : EventArgs
    {
        public byte[] Data { get; set; }
        public DataRecieveArgs(byte[] data)
        {
            Data = data;
        }

    }
}
