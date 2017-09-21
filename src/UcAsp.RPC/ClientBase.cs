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
        private static CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
        private readonly ILog _log = LogManager.GetLogger(typeof(ClientBase));

        private ConcurrentQueue<DataEventArgs> _clientTask = new ConcurrentQueue<DataEventArgs>();
        private ConcurrentDictionary<int, DataEventArgs> _resultTask = new ConcurrentDictionary<int, DataEventArgs>();
        private ConcurrentDictionary<int, DataEventArgs> _runingTask = new ConcurrentDictionary<int, DataEventArgs>();
        private List<ChannelPool> _channels = new List<ChannelPool>();
        public ConcurrentQueue<DataEventArgs> ClientTask { get { return this._clientTask; } set { this._clientTask = value; } }
        public ConcurrentDictionary<int, DataEventArgs> ResultTask { get { return this._resultTask; } set { this._resultTask = value; } }

        public ConcurrentDictionary<int, DataEventArgs> RuningTask { get { return this._runingTask; } set { this._runingTask = value; } }

        public List<ChannelPool> Channels { get { return this._channels; } set { this._channels = value; } }
        internal List<DataEventArgs> _timeourTask = new List<DataEventArgs>();

        public virtual void Run()
        {

            new Task(() =>
            {
                while (!cancelTokenSource.Token.IsCancellationRequested)
                {
                    DataEventArgs e;
                    if (ClientTask.TryPeek(out e))
                    {
                        if (_timeourTask.FirstOrDefault(o => o.TaskId == e.TaskId) != null)
                        {
                            ClientTask.TryDequeue(out e);
                            continue;
                        }

                        ChannelPool channel = Channels[0];
                        Dictionary<int, int> dic = GetChannel();
                        int k = dic.Count;
                        int cnum = new Random(unchecked((int)DateTime.Now.Ticks)).Next(k);
                        int i = 0;
                        if (dic.TryGetValue(cnum, out i))
                        {
                            channel = Channels[i];
                        }
                        else
                        {
                            int times = 0;
                            while (k == 0 && times < 5000)
                            {
                                dic = GetChannel();
                                k = dic.Count;
                                cnum = new Random(unchecked((int)DateTime.Now.Ticks)).Next(Channels.Count);
                                if (dic.TryGetValue(cnum, out i))
                                {
                                    channel = Channels[i];
                                    break;
                                }
                                Thread.Sleep(2);
                                times++;
                            }
                            if (times >= 5000 && Channels.Count < 50)
                            {
                                Channels.Add(new ChannelPool { IpPoint = channel.IpPoint });
                                i = Channels.Count - 1;
                            }
                            else
                            {
                                continue;
                            }
                        }


                        if (ClientTask.TryDequeue(out e))
                        {
                            Channels[i].ActiveHash = 1;
                            Channels[i].RunTimes++;
                            Channels[i].PingActives = DateTime.Now.Ticks;
                            e.CallHashCode = i;
                            Call(e, i);

                        }

                    }
                    else
                    {
                        Thread.Sleep(5);
                    }

                }

            }, TaskCreationOptions.LongRunning).Start();

        }


        public virtual void CallServiceMethod(DataEventArgs de)
        {
            if (ApplicationContext._taskId < int.MaxValue)
            {
                ApplicationContext._taskId++;
            }
            else
            {
                ApplicationContext._taskId = 0;
            }
            de.TaskId = ApplicationContext._taskId;

            try
            {
                ClientTask.Enqueue(de);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

        }
        public virtual DataEventArgs GetResult(DataEventArgs e)
        {
            DataEventArgs arg = new DataEventArgs();
            try
            {
                var cts = new CancellationTokenSource(150000);
                while (!cts.Token.IsCancellationRequested)
                {

                    if (ResultTask.TryGetValue(e.TaskId, out arg))
                    {
                        arg.StatusCode = StatusCode.Success;
                        Channels[arg.CallHashCode].ActiveHash = 0;
                        try
                        {
                            DataEventArgs outarg = new DataEventArgs();
                            ResultTask.TryRemove(arg.TaskId, out outarg);
                            RuningTask.TryRemove(arg.TaskId, out outarg);
                        }
                        catch { }
                        return arg;
                    }
                    Thread.Sleep(3);
                }
            }
            catch (Exception ex)
            {
                arg.StatusCode = StatusCode.Serious;
                _log.Error(ex);
                Console.WriteLine(ex);
                Channels[e.CallHashCode].ActiveHash = 1;
                return arg;

            }

            _timeourTask.Add(e);
            e.StatusCode = StatusCode.TimeOut;
            return e;


        }
        public virtual void Call(object obj, int len) { }
        public string Authorization { get; set; }


        public virtual void Exit()
        {
            cancelTokenSource.Cancel();


        }

        public abstract void CheckServer();

        public abstract DataEventArgs GetResult(DataEventArgs e, ChannelPool channel);
        private Dictionary<int, int> GetChannel()
        {
            Dictionary<int, int> dic = new Dictionary<int, int>();
            int num = 0;
            int k = 0;
            lock (Channels)
            {

                foreach (ChannelPool p in Channels)
                {
                    if (p.ActiveHash == 0 && p.Available == true)
                    {
                        dic.Add(k, num);
                        k++;
                    }
                    num++;
                }
            }
            return dic;
        }
    }
}
