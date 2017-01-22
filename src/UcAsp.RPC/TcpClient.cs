/***************************************************
*创建人:TecD02
*创建时间:2016/8/4 18:39:33
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
using System.Threading.Tasks;
using System.Diagnostics;
namespace UcAsp.RPC
{
    public class TcpClient : ClientBase
    {
        private static CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
        private readonly ILog _log = LogManager.GetLogger(typeof(TcpClient));
        private const int buffersize = 1024 * 5;
        private Dictionary<int, ChannelPool> runpool = new Dictionary<int, ChannelPool>();
        private System.Timers.Timer heatbeat = new System.Timers.Timer();
        public override void CallServiceMethod(object de)
        {
            lock (ClientTask)
            {
                DataEventArgs e = (DataEventArgs)de;
                if (ApplicationContext._taskId < int.MaxValue)
                {
                    ApplicationContext._taskId++;
                }
                else
                {
                    ApplicationContext._taskId = 0;
                }
                while (RuningTask.Count + 2 > IpAddress.Count)
                {
                    Thread.Sleep(2);
                }
                e.TaskId = ApplicationContext._taskId;
                ClientTask.Enqueue(e);
            }
        }

        public override void Connect(string ip, int port, int pool)
        {

            for (int i = 0; i < pool; i++)
            {
                try
                {
                    IPEndPoint ep = new IPEndPoint(IPAddress.Parse(ip), port);
                    Socket socket = Connect(ep);
                    if (socket != null)
                    {
                        ChannelPool channel = new ChannelPool() { Available = true, Client = socket, IpPoint = ep, PingActives = 0, RunTimes = 0 };
                        IpAddress.Add(channel);
                        Thread thgetResult = new Thread(new ParameterizedThreadStart(GetData));
                        thgetResult.Start(channel);
                    }
                    else
                    {
                        ChannelPool channel = new ChannelPool() { Available = false, Client = socket, IpPoint = ep, PingActives = 0, RunTimes = 0 };
                        IpAddress.Add(channel);
                    }

                }
                catch (Exception ex)
                {
                    _log.Error(ex);
                }
            }
        }

        private Socket Connect(IPEndPoint ip)
        {
            try
            {
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);// TcpConnect(ep);
                socket.Connect(ip);
                return socket;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        public override void Exit()
        {
            cancelTokenSource.Cancel();
        }

        public override DataEventArgs GetResult(DataEventArgs e)
        {
            int time = 0;
            Task<DataEventArgs> chektask = new Task<DataEventArgs>(() =>
            {
                while (true)
                {
                    if (cancelTokenSource.IsCancellationRequested)
                    {
                        return null;
                    }
                    Thread.Sleep(2);
                    DataEventArgs er = new DataEventArgs();
                    bool result = ResultTask.TryGetValue(e.TaskId, out er);
                    if (result)
                    {
                        return er;
                    }
                    if (time > 2000)
                    {
                        if (e.TryTimes < 6)
                        {
                            e.TryTimes++;
                            if (RuningTask.ContainsKey(e.TaskId) && !ResultTask.ContainsKey(e.TaskId))
                            {
                                RuningTask.Remove(e.TaskId);
                            }

                            ClientTask.Enqueue(e);
                        }
                        else
                        {

                            e.StatusCode = StatusCode.TimeOut;
                            return e;
                        }
                    }
                    time++;
                    TaskId = e.TaskId;

                }
            }, cancelTokenSource.Token);
            chektask.Start();
            DataEventArgs data = chektask.Result;
            RemovePool(data);

            return data;
        }
        public override void Run()
        {
            heatbeat.Interval = 30000;
            heatbeat.Elapsed -= Heatbeat_Elapsed;
            heatbeat.Elapsed += Heatbeat_Elapsed;
            heatbeat.Start();
            Task sendtask = new Task(() =>
            {

                while (!cancelTokenSource.IsCancellationRequested)
                {
                    Thread.Sleep(2);

                    List<ChannelPool> avipool = IpAddress.Where(o => o.ActiveHash == 0 && o.Client != null && o.Available == true).ToList();
                    if (avipool.Count < 3 && IpAddress.Count < 5)
                    {
                        Socket socket = Connect(avipool[0].IpPoint);
                        if (socket != null)
                        {
                            ChannelPool channel = new ChannelPool() { Available = true, Client = socket, IpPoint = avipool[0].IpPoint, PingActives = 0, RunTimes = 0, ActiveHash = 0 };
                            IpAddress.Add(channel);
                            Thread thgetResult = new Thread(new ParameterizedThreadStart(GetData));
                            thgetResult.Start(channel);
                        }
                    }
                    if (RuningTask.Count + 1 < avipool.Count)
                    {
                        if (ClientTask.Count > 0)
                        {
                            try
                            {
                                DataEventArgs e = ClientTask.Dequeue();
                                int len = e.TaskId % IpAddress.Count;
                                while (IpAddress[len].ActiveHash != 0 || IpAddress[len].Available == false)
                                {
                                    len++;
                                    if (len >= IpAddress.Count)
                                    {
                                        len = 0;
                                    }
                                }
                                IpAddress[len].ActiveHash = e.TaskId;
                                IpAddress[len].RunTimes++;
                                IpAddress[len].PingActives = DateTime.Now.Ticks;
                                Call(e, len);
                            }
                            catch (Exception ex) { _log.Error(ex); }

                        }

                    }

                }
            }, cancelTokenSource.Token);
            sendtask.Start();

        }
        private void Heatbeat_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            CheckServer();
        }


        private void GetData(object client)
        {
            while (!cancelTokenSource.IsCancellationRequested)
            {
                ChannelPool channcel = (ChannelPool)client;
                try
                {

                    Socket _client = channcel.Client;
                    ByteBuilder _recvBuilder = new ByteBuilder(buffersize);
                    byte[] buffer = new byte[buffersize];
                    int total = 0;
                    int timeO = 0;
                    while (!cancelTokenSource.IsCancellationRequested)
                    {

                        int l = _client.Receive(buffer, SocketFlags.None);
                        _recvBuilder.Add(buffer, 0, l);
                        total = _recvBuilder.GetInt32(0);

                        if (l == 0 && total == 0)
                        {
                            channcel.Available = false;
                            return;
                        }
                        if (total == _recvBuilder.Count)
                            break;
                    }
                    if (_recvBuilder.Count == 0)
                    {
                        Console.WriteLine("连接超时");

                    }
                    DataEventArgs dex = DataEventArgs.Parse(_recvBuilder);
                    dex.RemoteIpAddress = _client.RemoteEndPoint.ToString();
                    _log.Info(dex.ActionCmd + dex.ActionParam + ":" + dex.TaskId);
                    _client.ReceiveTimeout = int.MaxValue;
                    if (ResultTask.ContainsKey(dex.TaskId))
                    { ResultTask.Remove(dex.TaskId); }
                    ResultTask.Add(dex.TaskId, dex);


                }
                catch (Exception ex)
                {
                    CheckServer();
                    if (RuningTask.ContainsKey(channcel.ActiveHash) && !ResultTask.ContainsKey(channcel.ActiveHash))
                    {
                        ClientTask.Enqueue(RuningTask[channcel.ActiveHash]);
                        RuningTask.Remove(channcel.ActiveHash);
                    }
                    Console.WriteLine("GetData" + ex);
                    channcel.Available = false;
                    Thread.Sleep(100);
                    _log.Error(ex);
                    break;
                }
            }
        }
        private void Call(object obj, int len)
        {
            DataEventArgs e = (DataEventArgs)obj;
            try
            {
                if (RuningTask == null)
                {
                    RuningTask = new Dictionary<int, DataEventArgs>();
                }
                if (!RuningTask.ContainsKey(e.TaskId))
                {
                    RuningTask.Add(e.TaskId, e);
                }
                Socket _client = IpAddress[len].Client;

                byte[] _bf = e.ToByteArray();

                int sendlen = _client.Send(_bf, 0, _bf.Length, SocketFlags.None);

            }
            catch (Exception ex)
            {
                CheckServer();
                Console.WriteLine(ex);
                IpAddress[len].Available = false;
                _log.Error(ex + ex.StackTrace);
                e.StatusCode = StatusCode.TimeOut;
                e.TryTimes++;
                if (e.TryTimes < 3)
                {
                    RuningTask.Remove(e.TaskId);
                    ClientTask.Enqueue(e);
                    return;
                }
                ResultTask.Add(e.TaskId, e);
                return;
            }
        }

        public override void CheckServer()
        {
            Task t = new Task(() =>
            {
                if (heatbeat.Enabled)
                {
                    heatbeat.Enabled = false;
                    heatbeat.Stop();
                    try
                    {
                        for (int i = IpAddress.Count - 1; i >= 0; i--)
                        {
                            IPEndPoint address = IpAddress[i].IpPoint;
                            Socket client = Connect(address);
                            if (client != null)
                            {
                                if (!IpAddress[i].Available)
                                {
                                    IpAddress.RemoveAt(i);
                                    ChannelPool channel = new ChannelPool() { Available = true, Client = client, IpPoint = address, PingActives = 0, RunTimes = 0, ActiveHash = 0 };
                                    IpAddress.Add(channel);

                                    Thread thgetResult = new Thread(new ParameterizedThreadStart(GetData));
                                    thgetResult.Start(channel);
                                }
                                else
                                {
                                    client.Close();
                                }
                            }
                            else
                            {
                                for (int m = 0; m < IpAddress.Count; m++)
                                {
                                    IpAddress[i].Available = false;
                                    if (IpAddress[m].IpPoint.Address.ToString() == IpAddress[i].IpPoint.Address.ToString() && IpAddress[m].IpPoint.Port == IpAddress[i].IpPoint.Port)
                                    {
                                        IpAddress[m].Available = false;
                                        if (IpAddress[m].ActiveHash != 0)
                                        {
                                            if (RuningTask.ContainsKey(IpAddress[m].ActiveHash) && !ResultTask.ContainsKey(IpAddress[m].ActiveHash))
                                            {
                                                ClientTask.Enqueue(RuningTask[IpAddress[m].ActiveHash]);
                                                RuningTask.Remove(IpAddress[m].ActiveHash);
                                            }
                                            IpAddress[m].ActiveHash = 0;
                                        }
                                    }
                                }

                            }
                        }
                    }
                    catch (Exception ex)
                    { }
                    heatbeat.Enabled = true;
                    heatbeat.Start();
                }
            });
            t.Start();

        }
    }


}
