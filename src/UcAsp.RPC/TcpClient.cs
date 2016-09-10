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
namespace UcAsp.RPC
{
    public class TcpClient : IClient
    {
        private readonly ILog _log = LogManager.GetLogger(typeof(TcpClient));
        private ConcurrentQueue<DataEventArgs> _task = new ConcurrentQueue<DataEventArgs>();
        private ConcurrentQueue<DataEventArgs> _runtask = new ConcurrentQueue<DataEventArgs>();
        private const int buffersize = 1024 * 10;
        private Socket socket;
        public List<ChannelPool> IpAddress { get; set; }
        private List<ChannelPool> DisAddress { get; set; }

        public string LastError
        {
            get;

            set;
        }

        public DataEventArgs CallServiceMethod(DataEventArgs e)
        {

            DataEventArgs ex = Call(e);
            while (ex.ActionCmd == CallActionCmd.Error.ToString() && ex.TryTimes < 10)
            {
                e.ActionCmd = CallActionCmd.Call.ToString();
                ex = Call(e);
                // Console.WriteLine(ex.ActionCmd);
            }
            return ex;
        }

        private DataEventArgs Call(object obj)
        {
            DataEventArgs e = (DataEventArgs)obj;
            e.TryTimes = e.TryTimes + 1;
            try
            {
                Socket _client;
                if (e.ActionCmd == CallActionCmd.Ping.ToString())
                {
                    _client = PingTcpConnect(IpAddress);
                }
                else
                {
                    _client = TcpConnect(IpAddress);
                }
                e.CallHashCode = Convert.ToInt32(Task.CurrentId);
                ByteBuilder _recvBuilder = new ByteBuilder(buffersize);
                if (_client.Connected)
                {
                    byte[] _bf = e.ToByteArray();


                    _client.Send(_bf, 0, _bf.Length, SocketFlags.None);
                    _client.ReceiveTimeout = 180000;
                    byte[] buffer = new byte[buffersize];
                    int total = 0;

                    while (true)
                    {

                        int len = _client.ReceiveBufferSize;
                        buffer = new byte[len];
                        int l = _client.Receive(buffer);


                        _recvBuilder.Add(buffer, 0, l);
                        total = _recvBuilder.GetInt32(0);
                        //  Console.WriteLine(e.ActionParam+":"+_recvBuilder.Count+"."+total);
                        if (_recvBuilder.Count == total)
                        {

                            break;
                        }
                    }
                }
                DataEventArgs dex = DataEventArgs.Parse(_recvBuilder);
                dex.RemoteIpAddress = (IPEndPoint)_client.RemoteEndPoint;
                return dex;
            }
            catch (Exception ex)
            {


                _log.Error(ex);
                // Console.WriteLine(ex);
                e.ActionCmd = CallActionCmd.Error.ToString();
                return e;

            }



            // }
            //}
            //catch (Exception ext)
            //{
            //    _isconnect = false;
            //    DataEventArgs ex = new DataEventArgs { CallHashCode = (int)CallActionCmd.Error, ActionCmd = e.ActionCmd, ActionParam = e.ActionParam, HttpSessionId = e.HttpSessionId };
            //    return ex;

            //}
        }
        public void Connect(string ip, int port, int pool)
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Parse(ip), port);
            Socket socket = TcpConnect(ep);

        }
        private Socket TcpConnect(object obj)
        {
            List<ChannelPool> list = (List<ChannelPool>)obj;
            List<ChannelPool> avblist = list.FindAll(o => o.Available = true);
            if (avblist.Count <= 0)
                return null;
            int rad = new Random().Next(avblist.GetHashCode()) % list.Count;
            ChannelPool pool = avblist[rad];
            IPEndPoint ep = pool.IpAddress;
            try
            {
                Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                client.Connect(ep);
                pool.RunTimes++;
                pool.PingActives = DateTime.Now.Ticks;
                return client;
            }
            catch (Exception ex)
            {
                bool r = IpAddress.Remove(pool);
                if (r)
                {
                    if (DisAddress == null)
                    { DisAddress = new List<ChannelPool>(); }

                    DisAddress.Add(pool);
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
                        Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
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
