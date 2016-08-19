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
            // Console.WriteLine(_server.LocalEndPoint);

            /// while (true)
            //{
            //Socket socket = this._server.Accept();


            ThreadPool.QueueUserWorkItem(Accept, null);

            //}


        }
        private void Accept(object obj)
        {
            while (true)
            {
                Socket socket = this._server.Accept();
                _log.Info("连接：" + socket.LocalEndPoint);
                ThreadPool.QueueUserWorkItem(Recive, socket);
            }
        }
        private void Recive(object _server)
        {
            Socket socket = (Socket)(_server);
            while (true)
            {
                ByteBuilder _recvBuilder = new ByteBuilder(1024);
                if (socket.Connected)
                {
                    try
                    {
                        byte[] buffer = new byte[1024];
                        while (true)
                        {
                            int len = socket.Receive(buffer, 0, buffer.Length, SocketFlags.None);
                            _recvBuilder.Add(buffer);
                            if (len - 1024 <= 0)
                            { break; }
                        }
                        DataEventArgs e = DataEventArgs.Parse(_recvBuilder);

                        Call(socket, e);

                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex);
                        // Console.WriteLine(ex);
                        socket.Dispose();
                        Thread thread = Thread.CurrentThread;
                        thread.Abort();


                    }
                }
            }


        }





    }
}
