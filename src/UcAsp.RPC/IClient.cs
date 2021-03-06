﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Collections.Concurrent;
namespace UcAsp.RPC
{
    public interface IClient
    {
        ConcurrentQueue<DataEventArgs> ClientTask { get; set; }
        ConcurrentDictionary<int, DataEventArgs> ResultTask { get; set; }
        ConcurrentDictionary<int, DataEventArgs> RuningTask { get; set; }
        ConcurrentDictionary<int, TaskTicks> RunTime { get; set; }
        List<ChannelPool> Channels { get; set; }

        void AddClient(Config config, Dictionary<string, dynamic> proxyobj);

        ISerializer Serializer { get; }
        void CallServiceMethod(DataEventArgs de);

        DataEventArgs GetResult(DataEventArgs e);
        DataEventArgs GetResult(DataEventArgs e, ChannelPool channel);


        void Run();
        void Exit();

        string Authorization { get; set; }
    }
}
