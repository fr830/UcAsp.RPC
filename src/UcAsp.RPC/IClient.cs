using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Collections.Concurrent;
namespace UcAsp.RPC
{
    public interface IClient
    {
        bool IsStart { get; set; }
        Queue<DataEventArgs> ClientTask { get; set; }
        Dictionary<int, DataEventArgs> ResultTask { get; set; }
        Dictionary<int, DataEventArgs> RuningTask { get; set; }

        List<ChannelPool> IpAddress { get; set; }
        bool IsRun { get; set; }

        void Run();
        DataEventArgs GetResult(DataEventArgs e);
        int TaskId { get; set; }

        bool Connect(String ip, int port, int pool);

        void Exit();

        void CallServiceMethod(object e);

        string LastError { get; set; }

        long PingActives { get; set; }

        string Authorization { get; set; }
        void CheckServer();
    }
}
