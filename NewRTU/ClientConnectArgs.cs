using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace NewRTU
{
   public class ClientConnectArgs
    {
       EndPoint __remoteEndPoint;
        public EndPoint RemoteEndPoint
        {
            get { return __remoteEndPoint; }
        }
        public ClientConnectArgs(EndPoint endpoint)
        {
            __remoteEndPoint = endpoint;
        }
    }
}
