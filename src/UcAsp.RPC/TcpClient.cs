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
namespace UcAsp.RPC
{
    public class TcpClient : ClientBase
    {
        private readonly ILog _log = LogManager.GetLogger(typeof(TcpClient));
        private ConcurrentQueue<DataEventArgs> _task = new ConcurrentQueue<DataEventArgs>();
        private ConcurrentQueue<DataEventArgs> _runtask = new ConcurrentQueue<DataEventArgs>();
        private const int buffersize = 1024 * 10;
        private Socket _client;
        public List<ChannelPool> IpAddress { get; set; }
        private List<ChannelPool> DisAddress { get; set; }

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
            DataEventArgs ex = Call(e);
            while (ex.StatusCode != StatusCode.Success && ex.TryTimes < 5)
            {
                e.ActionCmd = ex.ActionCmd.ToString();
                e.LastError = "";
                e.TryTimes++;
                Thread.Sleep(10);
                ex = Call(e);
            }
            return ex;
        }

        private DataEventArgs Call(object obj)
        {
            DataEventArgs e = (DataEventArgs)obj;

            ByteBuilder _recvBuilder = new ByteBuilder(buffersize);
            e.CallHashCode = e.GetHashCode();
            try
            {

                if (e.ActionCmd == CallActionCmd.Ping.ToString())
                {
                    _client = PingTcpConnect(IpAddress);
                }
                else
                {
                    if (e.TryTimes > 1)
                    { _client = TcpConnect(IpAddress, true); }
                    else
                        _client = TcpConnect(IpAddress, false);
                }

                if (_client.Connected)
                {
                    byte[] _bf = e.ToByteArray();
                    _client.Send(_bf, 0, _bf.Length, SocketFlags.None);
                    // _client.ReceiveTimeout = 10000;
                    byte[] buffer = new byte[buffersize];
                    int total = 0;
                    int timeO = 0;
                    while (true)
                    {
                        int len = _client.ReceiveBufferSize;
                        buffer = new byte[len];
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
                return dex;
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                e.LastError = e.TaskId + ":" + e.ActionCmd + "." + e.ActionParam + ex.Message;
                e.StatusCode = StatusCode.Error;
                return e;

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
                    ChannelPool channel = new ChannelPool() { Available = true, Client = socket, IpAddress = ep, PingActives = 0, RunTimes = 0 };
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
            IPEndPoint ep = channel.IpAddress;
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
                    client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, false);
                    client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
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
                bool r = IpAddress.Remove(channel);
                if (r)
                {
                    if (DisAddress == null)
                    { DisAddress = new List<ChannelPool>(); }

                    DisAddress.Add(channel);
                }
                _log.Error(ex);
                return null;
            }
        }

        private Socket PingTcpConnect(object obj)
        {
            List<ChannelPool> list = (List<ChannelPool>)obj;

            if (DisAddress != null)
            {

                for (int i = 0; i < DisAddress.Count; i++)
                {
                    try
                    {
                        ChannelPool dpool = DisAddress[i];
                        IPEndPoint dep = dpool.IpAddress;
                        Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Rdm, ProtocolType.Tcp);
                        client.Connect(dep);
                        bool r = DisAddress.Remove(dpool);
                        if (r)
                        {
                            IpAddress.Add(dpool);
                        }
                    }
                    catch (Exception e)
                    {
                        _log.Error(e);
                    }
                }
            }


            int rad = new Random().Next(list.GetHashCode()) % list.Count;

            ChannelPool pool = list[rad];
            IPEndPoint ep = pool.IpAddress;
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



        public void Exit()
        {
            // Dispose();
        }

    }

}
