using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
namespace UcAsp.RPC
{
    public interface IClient
    {
        IPEndPoint IpAddress { get; set; }
        //   Queue<Socket> DicClient { get; set; }


        void Connect(String ip, int port, int pool);

        void Exit();

        DataEventArgs CallServiceMethod(DataEventArgs e);

        string LastError { get; set; }
    }
}
