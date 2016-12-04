/***************************************************
*创建人:TecD02
*创建时间:2016/8/4 18:12:48
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
using UcAsp.RPC;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using log4net;
using System.IO;
namespace UcAsp.RPC
{
    public class TcpServer : ServerBase
    {
        private readonly ILog _log = LogManager.GetLogger(typeof(TcpServer));
        private Socket _server;
        private Thread[] _startThread = new Thread[20];

        public override void StartListen(int port)
        {
            this.IsStart = true;
            _server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, true);
            this._server.Bind(new IPEndPoint(IPAddress.Any, port));
            this._server.Listen(3000);
            _log.Info("开启服务：" + port);
            for (int i = 0; i < 20; i++)
            {
                _startThread[i] = new Thread(new ParameterizedThreadStart(Start));
                _startThread[i].Start(null);
            }
            // ThreadPool.QueueUserWorkItem(Start,null);
        }

        private void Start(object obj)
        {
            while (true)
            {
                Socket client = _server.Accept();
                ThreadPool.QueueUserWorkItem(Accept, client);
            }
        }
        private void Accept(object socket)
        {
            Socket client = (Socket)socket;
            // while (true)
            // {
            try
            {
                Recive(client);
                // thread.Start();
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                Console.WriteLine(ex);
                IsStart = false;
                // break;
            }

            // }
        }
        private void Recive(object obj)
        {
            Socket client = (Socket)(obj);
            //  while (true)
            // {
            ByteBuilder _recvBuilder = new ByteBuilder(client.ReceiveBufferSize);
            if (client.Connected)
            {
                try
                {
                    byte[] buffer = new byte[buffersize];
                    int total = 0;
                    while (true)
                    {
                        int l = client.Receive(buffer);
                        _recvBuilder.Add(buffer, 0, l);
                        total = _recvBuilder.GetInt32(0);
                        if (_recvBuilder.Count == total)
                        { break; }
                    }
                    DataEventArgs e = DataEventArgs.Parse(_recvBuilder);
                    Console.WriteLine(e.ActionCmd + e.ActionParam);
                    Call(client, e);

                }
                catch (SocketException ex)
                {
                    //IsStart = false;
                    _log.Error(client.RemoteEndPoint + "断开服务" + ex.SocketErrorCode.ToString());
                    Console.WriteLine(client.RemoteEndPoint + "断开服务" + ex.SocketErrorCode.ToString());
                }
                finally
                {
                    //  client.Dispose();
                    // Thread thread = Thread.CurrentThread;
                    // if (thread.IsAlive) { thread.Abort(); }

                }
            }
            // }


        }
        public override void Stop()
        {
            IsStart = false;
            for (int i = 0; i < 20; i++)
            {
                if (_startThread[i] != null && _startThread[i].IsAlive)
                {
                    _startThread[i].Abort();
                }
            }
            _server.Dispose();
            _log.Error("服务退出");
        }

    }

}
