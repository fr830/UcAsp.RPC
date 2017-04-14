/***************************************************
*创建人:rixiang.yu
*创建时间:2017/3/14 16:28:27
*功能说明:<Function>
*版权所有:<Copyright>
*Frameworkversion:4.0
*CLR版本：4.0.30319.42000
***************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using log4net;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
namespace UcAsp.RPC
{
    public class SocketClient : ClientBase
    {
        private static CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
        private readonly ILog _log = LogManager.GetLogger(typeof(SocketClient));
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

        public override void CheckServer()
        {

        }

        public override bool Connect(string ip, int port, int pool)
        {
            bool flag = true;
            for (int i = 0; i < pool; i++)
            {
                try
                {
                    IPEndPoint ep = new IPEndPoint(IPAddress.Parse(ip), port);
                    Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    socket.Connect(ep);

                    if (socket != null)
                    {
                        ChannelPool channel = new ChannelPool() { Available = true, Client = socket, IpPoint = ep, PingActives = 0, RunTimes = 0 };
                        IpAddress.Add(channel);

                    }
                    else
                    {
                        ChannelPool channel = new ChannelPool() { Available = false, Client = socket, IpPoint = ep, PingActives = 0, RunTimes = 0 };
                        IpAddress.Add(channel);
                        flag = false;

                    }

                }
                catch (Exception ex)
                {
                    flag = false;
                    _log.Error(ex);
                }
            }
            return flag;
        }

        public override void Exit()
        {
            cancelTokenSource.Cancel();
            RuningTask.Clear();
            ResultTask.Clear();
            foreach (ChannelPool pool in IpAddress)
            {
                if (pool.Client != null && pool.Client.Connected)
                {
                    pool.Client.Close();
                }
            }
        }
        private void ReceiveCallback(IAsyncResult result)
        {
            StateObject state = (StateObject)result.AsyncState;
            Socket handler = state.workSocket;
            try
            {
                int bytesRead = handler.EndReceive(result);

                if (bytesRead > 0)
                {
                    state.Builder.Add(state.buffer, 0, bytesRead);
                    int total = state.Builder.GetInt32(0);

                    if (total == state.Builder.Count)
                    {
                        DataEventArgs dex = DataEventArgs.Parse(state.Builder);
                        lock (ResultTask)
                        {

                            ResultTask.AddOrUpdate(dex.TaskId, dex, (key, value) => value = dex);
                        }
                    }
                    else
                    {
                        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                lock (IpAddress)
                {
                    for (int i = IpAddress.Count - 1; i >= 0; i--)
                    {

                        ChannelPool pool = IpAddress[i];
                        if (pool != null)
                        {
                            IPEndPoint ip = (IPEndPoint)handler.RemoteEndPoint;
                            if (pool.IpPoint.Address.ToString() == ip.Address.ToString() && pool.IpPoint.Port == ip.Port)
                            {
                                IpAddress.Remove(pool);
                            }
                        }
                    }
                }

            }
            //result.AsyncWaitHandle.Close();
        }
        public override DataEventArgs GetResult(DataEventArgs e)
        {
            DataEventArgs outDea = new DataEventArgs();
            int time = 0;
            Task<DataEventArgs> chektask = new Task<DataEventArgs>(() =>
            {
                while (true)
                {
                    if (cancelTokenSource.IsCancellationRequested)
                    {
                        return null;
                    }
                    Thread.Sleep(5);
                    DataEventArgs er = new DataEventArgs();
                    bool result = ResultTask.TryGetValue(e.TaskId, out er);
                    if (result)
                    {
                        return er;
                    }

                    if (time > 15000)
                    {
                        if (e.TryTimes < 6)
                        {
                            e.TryTimes++;
                            lock (RuningTask)
                            {
                                if (RuningTask.ContainsKey(e.TaskId) && !ResultTask.ContainsKey(e.TaskId))
                                {
                                    RuningTask.TryRemove(e.TaskId, out outDea);
                                }
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
            Task run = new Task(() =>
            {
                while (!cancelTokenSource.IsCancellationRequested)
                {

                    List<ChannelPool> avipool = IpAddress.Where(o => o.ActiveHash == 0 && o.Client != null && o.Available == true).ToList();
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
                            catch (Exception ex)
                            {
                                _log.Error(ex);
                            }

                        }

                    }
                    Thread.Sleep(5);
                }

            }, cancelTokenSource.Token);
            run.Start();
        }
        public override void Run(DataEventArgs e, ChannelPool channel)
        {
            try
            {
                for (int i = 0; i < IpAddress.Count; i++)
                {
                    if (IpAddress[i].IpPoint == channel.IpPoint)
                    {
                        Call(e, i);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }
        private void Call(object obj, int len)
        {
            DataEventArgs outDea = new DataEventArgs();
            DataEventArgs e = (DataEventArgs)obj;
            try
            {
                if (RuningTask == null)
                {
                    RuningTask = new System.Collections.Concurrent.ConcurrentDictionary<int, DataEventArgs>();
                }

                RuningTask.AddOrUpdate(e.TaskId, e, (key, value) => value = e);

                Socket _client = IpAddress[len].Client;

                byte[] _bf = e.ToByteArray();


                _client.BeginSend(_bf, 0, _bf.Length, SocketFlags.None, null, null);
                StateObject state = new StateObject();
                state.workSocket = _client;
                SocketError error;
                _client.BeginReceive(state.buffer, 0, StateObject.BufferSize, SocketFlags.None, out error, ReceiveCallback, state);
                // Console.WriteLine(error);
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
                    RuningTask.TryRemove(e.TaskId, out outDea);
                    for (int i = 0; i < IpAddress.Count; i++)
                    {
                        if (IpAddress[i].ActiveHash == e.TaskId)
                        {
                            IpAddress[i].ActiveHash = 0;
                        }
                    }
                    ClientTask.Enqueue(e);
                    return;
                }
                ResultTask.AddOrUpdate(e.TaskId, e, (key, value) => value = e);
                return;
            }
        }

    }

}
