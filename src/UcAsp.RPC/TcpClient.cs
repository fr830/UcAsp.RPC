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
                   // Console.WriteLine(RuningTask.Count + "." + ClientTask.Count);
                    if (result)
                    {
                        return er;
                    }
                    if (time > 2000)
                    {
                        if (e.TryTimes < 6)
                        {
                            e.TryTimes++;
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
            Task sendtask = new Task(() =>
            {

                while (!cancelTokenSource.IsCancellationRequested)
                {
                    Thread.Sleep(2);

                    List<ChannelPool> avipool = IpAddress.Where(o => o.ActiveHash == 0 && o.Client != null).ToList();
                    if (avipool.Count < 2 && IpAddress.Count < 5)
                    {
                        Socket socket = Connect(IpAddress[0].IpPoint);
                        if (socket != null)
                        {
                            ChannelPool channel = new ChannelPool() { Available = true, Client = socket, IpPoint = IpAddress[0].IpPoint, PingActives = 0, RunTimes = 0, ActiveHash = 0 };
                            IpAddress.Add(channel);
                            Thread thgetResult = new Thread(new ParameterizedThreadStart(GetData));
                            thgetResult.Start(channel);
                        }
                    }
                    if (RuningTask.Count + 1 < IpAddress.Count)
                    {
                        if (ClientTask.Count > 0)
                        {
                            try
                            {
                                DataEventArgs e = ClientTask.Dequeue();
                                int len = e.TaskId % IpAddress.Count;
                                while (IpAddress[len].ActiveHash != 0)
                                {
                                    len++;
                                    if (len > IpAddress.Count)
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

        private void RemovePool(DataEventArgs hash)
        {
            if (ResultTask.ContainsKey(hash.CallHashCode))
            {
                ResultTask.Remove(hash.CallHashCode);
            }

            if (RuningTask.ContainsKey(hash.TaskId))
            {

                RuningTask.Remove(hash.TaskId);
            }
            for (int i = 0; i < IpAddress.Count; i++)
            {
                if (IpAddress[i].ActiveHash == hash.TaskId)
                {
                    //  Console.WriteLine("RemovePool:" + hash.TaskId + "X");
                    IpAddress[i].ActiveHash = 0;
                }
            }
        }

        private void GetData(object client)
        {
            while (true)
            {
                try
                {
                    lock (client)
                    {
                        ChannelPool channcel = (ChannelPool)client;
                        Socket _client = channcel.Client;
                        ByteBuilder _recvBuilder = new ByteBuilder(buffersize);
                        byte[] buffer = new byte[buffersize];
                        int total = 0;
                        int timeO = 0;
                        while (true)
                        {

                            int l = _client.Receive(buffer, SocketFlags.None);
                            _recvBuilder.Add(buffer, 0, l);
                            total = _recvBuilder.GetInt32(0);

                            if (l == 0 && total == 0)
                            {
                                timeO++;
                                Thread.Sleep(5);
                                if (timeO > 1000)
                                    break;
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
                        ResultTask.Add(dex.TaskId, dex);
                    }

                }
                catch (Exception ex)
                {
                    Thread.Sleep(100);
                    _log.Error(ex);
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
                RuningTask.Add(e.TaskId, e);

                Socket _client = IpAddress[len].Client;

                byte[] _bf = e.ToByteArray();
                // Console.WriteLine(e.TaskId + ".");

                int sendlen = _client.Send(_bf, 0, _bf.Length, SocketFlags.None);
                //   Console.WriteLine(e.TaskId + "." + sendlen + "==" + _bf.Length);
            }
            catch (Exception ex)
            {
                _log.Error(ex + ex.StackTrace);
                e.StatusCode = StatusCode.TimeOut;
                e.TryTimes++;
                if (e.TryTimes < 3)
                {
                    ClientTask.Enqueue(e);
                    return;
                }
                ResultTask.Add(e.TaskId, e);
                return;
            }
        }


    }


}
