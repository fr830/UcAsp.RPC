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

        public override void CallServiceMethod(object de)
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
            e.TaskId = ApplicationContext._taskId;
            ClientTask.Enqueue(e);
        }

        public override void Connect(string ip, int port, int pool)
        {

            // if (pool > 5)
            //    pool = 5;
            for (int i = 0; i < pool; i++)
            {
                try
                {
                    IPEndPoint ep = new IPEndPoint(IPAddress.Parse(ip), port);
                    Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);// TcpConnect(ep);
                    socket.Connect(ep);
                    ChannelPool channel = new ChannelPool() { Available = true, Client = socket, IpPoint = ep, PingActives = 0, RunTimes = 0 };
                    IpAddress.Add(channel);


                    Thread thgetResult = new Thread(new ParameterizedThreadStart(GetData));
                    thgetResult.Start(channel);

                }
                catch (Exception ex)
                {
                    _log.Error(ex);
                }
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
                    Thread.Sleep(10);
                    DataEventArgs er = new DataEventArgs();
                    bool result = ResultTask.TryGetValue(e.TaskId, out er);
                    if (result)
                    {
                        ResultTask.Remove(e.TaskId);
                        return er;
                    }
                    if (time > 1500)
                    {
                        e.StatusCode = StatusCode.TimeOut;
                        return e;
                    }
                    time++;
                    TaskId = e.TaskId;

                }
            }, cancelTokenSource.Token);
            chektask.Start();
            return chektask.Result;
        }

        public override void Run()
        {
            Task sendtask = new Task(() =>
            {
                while (!cancelTokenSource.IsCancellationRequested)
                {
                    Thread.Sleep(10);

                    if (ClientTask.Count > 0)
                    {
                        try
                        {
                            DataEventArgs e = ClientTask.Dequeue();
                            Call(e);
                        }
                        catch (Exception ex) { _log.Error(ex); }

                    }

                }
            }, cancelTokenSource.Token);
            sendtask.Start();

        }


        private void GetData(object client)
        {
            while (true)
            {
                try
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
                catch (Exception ex)
                {
                    Thread.Sleep(100);
                    _log.Error(ex);
                }
            }
        }
        private void Call(object obj)
        {
            DataEventArgs e = (DataEventArgs)obj;
            e.CallHashCode = e.GetHashCode();
            try
            {
                int len = e.TaskId % IpAddress.Count;
                Socket _client = IpAddress[len].Client;
                while (_client == null)
                {
                    if (len + 1 < IpAddress.Count)
                    {
                        _client = IpAddress[len + 1].Client;
                        len = len + 1;
                    }
                    else
                    {
                        len = 0;
                    }
                }
                //if (_client.Poll(100, SelectMode.SelectError))
                //{
                //    Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);// TcpConnect(ep);
                //    socket.Connect(IpAddress[e.TaskId % IpAddress.Count].IpPoint);
                //    IpAddress[e.TaskId % IpAddress.Count].Client = socket;
                //}
                byte[] _bf = e.ToByteArray();
                _client.Send(_bf, 0, _bf.Length, SocketFlags.None);
            }
            catch (Exception ex)
            {
                _log.Error(ex + ex.StackTrace);
                e.StatusCode = StatusCode.TimeOut;
                e.TryTimes++;
                if (e.TryTimes < 3)
                {
                    Thread.Sleep(50);
                    Call(e);
                    return;
                }
                ResultTask.Add(e.TaskId, e);
                return;
            }
        }

    }


}
