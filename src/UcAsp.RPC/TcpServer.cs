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

        public override void StartListen(int port)
        {
            _server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this._server.Bind(new IPEndPoint(IPAddress.Any, port));
            this._server.Listen(3000);
            _log.Info("开启服务：" + port);
            for (int i = 0; i < 10; i++)
            {
                Thread th = new Thread(new ParameterizedThreadStart(Accept));
                th.Start(_server);
                //ThreadPool.QueueUserWorkItem(Accept, _server);

            }


        }
        private void Accept(object obj)
        {
            while (true)
            {
                Socket server = (Socket)obj;
                Socket socket = server.Accept();
                ThreadPool.QueueUserWorkItem(Recive, socket);
            }
        }
        private void Recive(object obj)
        {
            Socket socket = (Socket)(obj);
            while (true)
            {
                ByteBuilder _recvBuilder = new ByteBuilder(socket.ReceiveBufferSize);
                if (socket.Connected)
                {
                    try
                    {
                        byte[] buffer = new byte[buffersize];
                        int total = 0;
                        while (true)
                        {
                            int len = socket.ReceiveBufferSize;
                            buffer = new byte[len];
                            int l = socket.Receive(buffer);
                            _recvBuilder.Add(buffer, 0, l);
                            total = _recvBuilder.GetInt32(0);
                            //Thread.Sleep(1);
                            if (_recvBuilder.Count == total)
                            { break; }
                        }
                        DataEventArgs e = DataEventArgs.Parse(_recvBuilder);

                        Call(socket, e);

                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex);
                        Console.WriteLine(socket.RemoteEndPoint + "断开服务");

                    }
                    finally
                    {
                        socket.Dispose();
                        Thread thread = Thread.CurrentThread;
                        thread.Abort();
                    }
                }
            }


        }


    }

}
