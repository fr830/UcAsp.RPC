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
using System.Dynamic;
using log4net;

namespace UcAsp.RPC
{
    public abstract class ClientBase : IClient
    {
        private static CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
        private readonly ILog _log = LogManager.GetLogger(typeof(ClientBase));
        private Config _config;
        public virtual ISerializer Serializer { get { return new ProtoSerializer(); } }
        private ConcurrentQueue<DataEventArgs> _clientTask = new ConcurrentQueue<DataEventArgs>();
        private ConcurrentDictionary<int, DataEventArgs> _resultTask = new ConcurrentDictionary<int, DataEventArgs>();
        private ConcurrentDictionary<int, DataEventArgs> _runingTask = new ConcurrentDictionary<int, DataEventArgs>();
        private List<ChannelPool> _channels = new List<ChannelPool>();
        public ConcurrentQueue<DataEventArgs> ClientTask { get { return this._clientTask; } set { this._clientTask = value; } }
        public ConcurrentDictionary<int, DataEventArgs> ResultTask { get { return this._resultTask; } set { this._resultTask = value; } }

        public ConcurrentDictionary<int, DataEventArgs> RuningTask { get { return this._runingTask; } set { this._runingTask = value; } }

        public List<ChannelPool> Channels { get { return this._channels; } set { this._channels = value; } }

        protected List<DataEventArgs> _timeoutTask = new List<DataEventArgs>();

        /// <summary>
        /// 监控运算中的性能
        /// </summary>

        public ConcurrentDictionary<int, TaskTicks> RunTime { get; set; }
        internal ConcurrentDictionary<int, StateObject> RunStateObjects = new ConcurrentDictionary<int, StateObject>();

        public virtual void AddClient(Config config, Dictionary<string, dynamic> proxyobj)
        {
            _config = config; string[] ipport = ((string)config.GetValue("server", "ip")).Split(';');
            int pool = Convert.ToInt32(config.GetValue("server", "pool", 2));
            for (int i = 0; i < ipport.Length; i++)
            {

                if (ipport[i].Split(':').Length > 1)
                {

                    string[] ip = ipport[i].Split(':');
                    ServerPort port = new ServerPort() { Ip = ip[0], Port = int.Parse(ip[1]), Pool = pool };
                    addClient(port, config, proxyobj);

                }
            }
            string[] relation = config.GetEntryNames("relation");
            if (relation != null)
            {
                Proxy.RelationDll = new Dictionary<string, string>();
                foreach (string va in relation)
                {

                    if (!Proxy.RelationDll.ContainsKey(va))
                    {
                        object rdll = config.GetValue("relation", va);
                        if (rdll != null)
                        {
                            Proxy.RelationDll.Add(va, rdll.ToString());
                        }

                    }
                }
            }
        }

        private void addClient(object serverport, Config config, Dictionary<string, dynamic> proxobj)
        {


            ServerPort sport = (ServerPort)serverport;
            string ip = sport.Ip;
            int port = sport.Port;
            int pool = sport.Pool;

            IClient _client;

            string password = (string)config.GetValue("server", "password");
            string username = (string)config.GetValue("server", "username");


            ChannelPool channlepool = new ChannelPool { IpPoint = new IPEndPoint(IPAddress.Parse(ip), port), Available = true };
            this.Channels.Add(channlepool);
            this.Authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
            DataEventArgs vali = new DataEventArgs { ActionCmd = CallActionCmd.Validate.ToString(), ActionParam = this.Authorization, TaskId = 0 };
            DataEventArgs valiresult = this.GetResult(vali, channlepool);
            if (valiresult.StatusCode != StatusCode.Success)///如果验证失败退出
                return;


            this.Authorization = valiresult.HttpSessionId;

            DataEventArgs callreg = new DataEventArgs() { HttpSessionId = this.Authorization, ActionCmd = CallActionCmd.Register.ToString(), ActionParam = "Register", T = typeof(List<RegisterInfo>) };


            DataEventArgs reg = this.GetResult(callreg, channlepool);

            if (reg.StatusCode != StatusCode.Success)
                return;

            this.Run();
            List<RegisterInfo> registerInfos = new List<RegisterInfo>();
            if (!string.IsNullOrEmpty(reg.Json))
            {
                registerInfos = Serializer.ToEntity<List<RegisterInfo>>(reg.Json);
            }
            else
            {
                registerInfos = Serializer.ToEntity<List<RegisterInfo>>(reg.Binary);
            }
            if (registerInfos != null)
            {

                for (int i = 0; i < pool; i++)
                {
                    ChannelPool channel = new ChannelPool { IpPoint = new IPEndPoint(IPAddress.Parse(ip), port), Available = true };
                    this.Channels.Add(channel);
                }
                foreach (RegisterInfo info in registerInfos)
                {

                    lock (proxobj)
                    {
                        try
                        {
                            string assname = string.Format("{0}.{1}", info.FaceNameSpace, info.InterfaceName);

                            dynamic val = new { ClassName = info.ClassName, NameSpace = info.NameSpace, Client = this };
                            if (!proxobj.ContainsKey(assname))
                            {
                                proxobj.Add(assname, val);
                            }
                            else
                            {
                                bool pc = false;
                                foreach (ChannelPool p in proxobj[assname].Client.Channels)
                                {
                                    if (p.IpPoint == channlepool.IpPoint)
                                    {
                                        pc = true;
                                    }
                                }
                                if (!pc)
                                {
                                    proxobj[assname].Client.Channels.Add(channlepool);
                                    for (int i = 0; i < 10; i++)
                                    {
                                        ChannelPool channel = new ChannelPool { IpPoint = new IPEndPoint(IPAddress.Parse(ip), port), Available = true };
                                        proxobj[assname].Client.Channels.Add(channel);
                                    }
                                }

                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    }
                }
            }

        }


        public virtual void Run()
        {

            new Task(runworker =>
             {
                 while (!cancelTokenSource.Token.IsCancellationRequested)
                 {
                     DataEventArgs e;
                     if (ClientTask.TryPeek(out e))
                     {
                         lock (_timeoutTask)
                         {
                             try
                             {
                                 if (_timeoutTask.FirstOrDefault(o => o.TaskId == e.TaskId) != null)
                                 {
                                     ClientTask.TryDequeue(out e);
                                     continue;
                                 }
                             }
                             catch (Exception ex)
                             {
                                 Console.WriteLine(ex);
                             }
                         }



                         try
                         {

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
                                 while (k == 0 && times < 500)
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
                                 if (times >= 500 && Channels.Count < 50)
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
                         catch (Exception ex)
                         {
                             Console.WriteLine(ex);
                             _log.Error(ex);
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
                if (RunTime == null)
                    RunTime = new ConcurrentDictionary<int, TaskTicks>();
                ClientTask.Enqueue(de);
                RunTime.TryAdd(de.TaskId, new TaskTicks() { InitTime = DateTime.Now.Ticks, IntoQueTime = 0, OutQueTime = 0 });

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
                var cts = new CancellationTokenSource(_config.GetValue("server", "timeout", 15) * 1000);
                while (!cts.Token.IsCancellationRequested)
                {

                    if (ResultTask.TryGetValue(e.TaskId, out arg))
                    {
                        arg.StatusCode = StatusCode.Success;
                        Channels[arg.CallHashCode].ActiveHash = 0;
                        try
                        {
                            StateObject oo = new StateObject();
                            RunStateObjects.TryRemove(arg.TaskId, out oo);
                            if (oo != null)
                                oo.Builder.ReSet();
                            DataEventArgs outarg = new DataEventArgs();
                            ResultTask.TryRemove(arg.TaskId, out outarg);
                            RuningTask.TryRemove(arg.TaskId, out outarg);

                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                        try
                        {
                            TaskTicks time = new TaskTicks();
                            if (RunTime.TryGetValue(arg.TaskId, out time))
                            {

                                time.EndTime = DateTime.Now.Ticks;
                                RunTime.TryUpdate(arg.TaskId, time, time);
                                _log.Debug("RunTime:\r\n" + arg + "\r\n" + time + "\r\n");
                            }

                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
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
                Channels[e.CallHashCode].ActiveHash = 0;
                return arg;

            }
            _log.Info(e.TaskId + "超时");
            _timeoutTask.Add(e);
            e.StatusCode = StatusCode.TimeOut;
            return e;


        }
        public virtual void Call(object obj, int len) { }
        public string Authorization { get; set; }


        public abstract void Exit();

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
                    if ((p.ActiveHash == 0 && p.Available == true))
                    {
                        dic.Add(k, num);
                        k++;
                    }
                    if ((p.ActiveHash != 0 && p.Available == true))
                    {
                        if (DateTime.Now.Ticks - p.PingActives > (1000 * 10000))
                        {
                            dic.Add(k, num);
                            k++;
                        }
                    }
                    num++;
                }
            }
            return dic;
        }

    }

    public class TaskTicks
    {
        public long InitTime { get; set; }

        public long IntoQueTime { get; set; }

        public long OutQueTime { get; set; }

        public long EndTime { get; set; }
    }
}
