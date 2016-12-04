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
        private readonly ILog _log = LogManager.GetLogger(typeof(TcpClient));
        private ConcurrentQueue<DataEventArgs> _task = new ConcurrentQueue<DataEventArgs>();
        private ConcurrentQueue<DataEventArgs> _runtask = new ConcurrentQueue<DataEventArgs>();
        private const int buffersize = 1024;
        private Socket _client;
        public List<ChannelPool> IpAddress { get; set; }
        public string LastError
        {
            get;

            set;
        }

        public override DataEventArgs CallServiceMethod(object de)
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
            Stopwatch wath = new Stopwatch();
            wath.Start();
            DataEventArgs ex = Call(e);
            while (ex.StatusCode != StatusCode.Success && ex.TryTimes < 5)
            {
                if (ex.StatusCode == StatusCode.Serious)
                    break;
                e.ActionCmd = ex.ActionCmd.ToString();
                e.LastError = "";
                e.TryTimes++;
                Thread.Sleep(10);
                ex = Call(e);
            }
            wath.Stop();
            _log.Info(e.ActionParam + ":" + e.CallHashCode + ":" + e.TaskId + ":" + wath.ElapsedMilliseconds);
            //if (!_client.Connected)
            //{
            //    _client.Close();
            //}
            return ex;
        }

        private DataEventArgs Call(object obj)
        {

            DataEventArgs e = (DataEventArgs)obj;
            PingActives = (DateTime.Now.Ticks / 10000) / 1000;
            ByteBuilder _recvBuilder = new ByteBuilder(buffersize);
            e.CallHashCode = e.GetHashCode();
            try
            {

                //if (e.ActionCmd == CallActionCmd.Ping.ToString())
                //{
                //    _client = PingTcpConnect(IpAddress);
                //}
                //else
                //{
                //if (e.TryTimes > 1)
                //{ _client = TcpConnect(IpAddress, true); }
                // else
                _client = TcpConnect(IpAddress, true);
                //}

                if (_client.Connected)
                {
                    byte[] _bf = e.ToByteArray();
                    _client.Send(_bf, 0, _bf.Length, SocketFlags.None);
                    _client.ReceiveTimeout = 180000;
                    byte[] buffer = new byte[buffersize];
                    int total = 0;
                    int timeO = 0;
                    while (true)
                    {
                        int l = _client.Receive(buffer);

                        _recvBuilder.Add(buffer, 0, l);
                        total = _recvBuilder.GetInt32(0);

                        if (l == 0 && total == 0)
                        {
                            timeO++;
                            Thread.Sleep(10);
                            if (timeO > 1000)
                                break;
                        }
                        if (total == _recvBuilder.Count)
                            break;
                    }
                }

                if (_recvBuilder.Count == 0)
                {
                    Console.WriteLine("连接超时");

                }
                DataEventArgs dex = DataEventArgs.Parse(_recvBuilder);
                dex.RemoteIpAddress = _client.RemoteEndPoint.ToString();
                _log.Info(dex.ActionCmd + dex.ActionParam + ":" + dex.TaskId);
                return dex;
            }
            catch (Exception ex)
            {
                _log.Error(e.TaskId + ":" + e.ActionCmd + "." + e.ActionParam + ex.Message);

                e.LastError = e.TaskId + ":" + e.ActionCmd + "." + e.ActionParam + ex.Message;
                e.StatusCode = StatusCode.Error;
                return e;

            }
            finally
            {
                if (_client != null)
                    _client.Dispose();
            }


        }
        public override void Connect(string ip, int port, int pool)
        {
            IpAddress = new List<ChannelPool>();
            if (pool > 5)
                pool = 5;
            for (int i = 0; i < pool; i++)
            {
                try
                {
                    IPEndPoint ep = new IPEndPoint(IPAddress.Parse(ip), port);
                    Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);// TcpConnect(ep);
                    socket.Connect(ep);
                    ChannelPool channel = new ChannelPool() { Available = true, Client = socket, IpPoint = ep, PingActives = 0, RunTimes = 0 };
                    IpAddress.Add(channel);
                }
                catch (Exception ex)
                {
                    _log.Error(ex);
                }
            }

        }
        private Socket TcpConnect(object obj, bool reconnect)
        {

            List<ChannelPool> list = (List<ChannelPool>)obj;
            List<ChannelPool> avblist = list.FindAll(o => o.Available = true);
            if (avblist.Count <= 0)
                return null;
            int rad = new Random().Next(avblist.GetHashCode()) % list.Count;
            ChannelPool channel = avblist[rad];
            IPEndPoint ep = channel.IpPoint;
            try
            {
                if (!reconnect && channel.Client != null && channel.Client.Connected)
                {
                    Socket client = channel.Client;
                    //client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, false);
                    //client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    channel.RunTimes++;
                    channel.PingActives = DateTime.Now.Ticks;
                    return client;
                }
                else
                {
                    Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    //  client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, false);
                    client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, true);
                    client.Connect(ep);
                    channel.RunTimes++;
                    channel.PingActives = DateTime.Now.Ticks;
                    channel.Client = client;
                    IpAddress = list;

                    return client;
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                return null;
            }
        }

        private Socket PingTcpConnect(object obj)
        {
            List<ChannelPool> list = (List<ChannelPool>)obj;

            if (list.Count > 0)
            {
                int rad = new Random().Next(list.GetHashCode()) % list.Count;

                ChannelPool pool = list[rad];
                IPEndPoint ep = pool.IpPoint;
                try
                {
                    Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    client.Connect(ep);
                    pool.RunTimes++;
                    pool.PingActives = DateTime.Now.Ticks;
                    pool.Available = true;

                    return client;
                }
                catch (Exception ex)
                {
                    pool.Available = false;
                    _log.Error(ex);
                    return null;
                }

            }
            else
            {
                return null;
            }
        }



        public override void Exit()
        {
            foreach (ChannelPool pool in IpAddress)
            {
                if (pool.Client.Connected)
                {
                    pool.Client.Close();
                }
            }
        }

    }


}
