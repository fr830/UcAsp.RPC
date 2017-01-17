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
        private bool stop = false;

        public override void StartListen(int port)
        {
            this.IsStart = true;
            _server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, true);
            this._server.Bind(new IPEndPoint(IPAddress.Any, port));
            this._server.Listen(3000);
            _log.Info("开启服务：" + port);
            #region 异步
            //this._server.BeginAccept(new AsyncCallback(Accept), this._server);

            #endregion
            for (int i = 0; i < Environment.ProcessorCount*2; i++)
            {
                _startThread[i] = new Thread(new ParameterizedThreadStart(Start));
                _startThread[i].Start(null);
            }
            // ThreadPool.QueueUserWorkItem(Start,null);
        }
        #region  同步

        private void Start(object obj)
        {
            while (true)
            {
                Socket client = _server.Accept();
                //  client.ReceiveAsync(SocketAsyncEventArgs

                // ThreadPool.SetMaxThreads(10, 10);

                Thread t = new Thread(new ParameterizedThreadStart(Accept));
                t.Start(client);
                // ThreadPool.QueueUserWorkItem(Accept, client);
                if (stop)
                    break;
            }
        }
        private void Accept(object socket)
        {
            Socket client = (Socket)socket;
            // while (true)
            // {
            try
            {
                Thread t = new Thread(new ParameterizedThreadStart(Recive));
                t.Start(client);
                // Recive(client);
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
            while (true)
            {
                if (stop)
                    break;

                if (client.Connected)
                {
                    try
                    {
                      //  Console.WriteLine(client.LocalEndPoint);
                        ByteBuilder _recvBuilder = new ByteBuilder(client.ReceiveBufferSize);
                        byte[] buffer = new byte[buffersize];
                        int total = 0;
                        while (true)
                        {
                            int l = client.Receive(buffer, SocketFlags.None);
                            _recvBuilder.Add(buffer, 0, l);
                            total = _recvBuilder.GetInt32(0);
                            if (_recvBuilder.Count == total)
                            { break; }
                        }
                        DataEventArgs e = DataEventArgs.Parse(_recvBuilder);
                        Console.WriteLine(e.TaskId+".");
                        Call(client, e);



                    }
                    catch (SocketException ex)
                    {
                        _log.Error(client.RemoteEndPoint + "断开服务" + ex.SocketErrorCode.ToString());
                        Console.WriteLine(client.RemoteEndPoint + "断开服务" + ex.SocketErrorCode.ToString());
                        client.Dispose();
                        Thread thread = Thread.CurrentThread;
                        if (thread.IsAlive) { thread.Abort(); }
                    }
                    finally
                    {
                        //client.Dispose();
                        // Thread thread = Thread.CurrentThread;
                        // if (thread.IsAlive) { thread.Abort(); }

                    }
                }
                else
                {
                    break;
                }

            }


        }
        #endregion
        #region  异步
        void Accept(IAsyncResult iar)
        {
            //还原传入的原始套接字
            Socket client = (Socket)iar.AsyncState;

            //在原始套接字上调用EndAccept方法，返回新的套接字
            // Socket service = MyServer.EndAccept(iar);
        }
        #endregion
        public override void Stop()
        {
            IsStart = false;
            stop = true;
            for (int i = 0; i < 20; i++)
            {
                if (_startThread[i] != null && _startThread[i].IsAlive)
                {
                    _startThread[i].Abort();
                }
            }
            GC.SuppressFinalize(this);
            _server.Dispose();
            Console.WriteLine("服务退出");
            _log.Error("服务退出");
        }

    }

}
