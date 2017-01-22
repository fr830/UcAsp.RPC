/***************************************************
*创建人:TecD02
*创建时间:2016/11/11 22:33:20
*功能说明:<Function>
*版权所有:<Copyright>
*Frameworkversion:4.0
*CLR版本：4.0.30319.42000
***************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Concurrent;
using log4net;

namespace UcAsp.RPC
{
    public abstract class ClientBase : IClient
    {
        public Queue<DataEventArgs> ClientTask { get; set; }
        public Dictionary<int, DataEventArgs> ResultTask { get; set; }

        public Dictionary<int, DataEventArgs> RuningTask { get; set; }
        private Socket socket;
        public List<ChannelPool> IpAddress { get; set; }
        public int TaskId { get; set; }
        public abstract void Run();
        public bool IsStart
        {
            get;

            set;
        }

        public string LastError
        {
            get;

            set;
        }

        public bool IsRun
        {
            get;

            set;
        }

        public long PingActives
        {
            get;

            set;
        }

        public abstract void CallServiceMethod(object e);
        public abstract DataEventArgs GetResult(DataEventArgs e);

        public abstract void Connect(string ip, int port, int pool);


        public void RemovePool(DataEventArgs hash)
        {
            if (ResultTask.ContainsKey(hash.TaskId))
            {
                ResultTask.Remove(hash.TaskId);
            }

            if (RuningTask.ContainsKey(hash.TaskId))
            {
                for (int i = 0; i < IpAddress.Count; i++)
                {
                    if (IpAddress[i].ActiveHash == hash.TaskId)
                    {
                        IpAddress[i].ActiveHash = 0;
                    }
                }
                RuningTask.Remove(hash.TaskId);
            }
            for (int i = 0; i < IpAddress.Count; i++)
            {
                if (IpAddress[i].ActiveHash == hash.TaskId)
                {
                    IpAddress[i].ActiveHash = 0;
                }
            }
        }
        public string Authorization { get; set; }

        public abstract void Exit();

        public abstract void CheckServer();
    }
}
