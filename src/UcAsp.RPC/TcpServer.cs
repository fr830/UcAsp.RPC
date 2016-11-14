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
            this.IsStart = true;
            _server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, true);
            this._server.Bind(new IPEndPoint(IPAddress.Any, port));
            this._server.Listen(3000);
            _log.Info("开启服务：" + port);
            int coreCount = 2;
            foreach (var item in new System.Management.ManagementObjectSearcher("Select * from Win32_Processor").Get())
            {
                coreCount += int.Parse(item["NumberOfLogicalProcessors"].ToString());
            }
            for (int i = 0; i < coreCount; i++)
            {
                Thread th = new Thread(new ParameterizedThreadStart(Accept));
                th.Start(_server);

            }

        }
        private void Accept(object obj)
        {
            while (true)
            {
                try
                {
                    Socket server = (Socket)obj;
                    Socket socket = server.Accept();
                    ThreadPool.QueueUserWorkItem(Recive, socket);
                }
                catch
                {
                    IsStart = false;
                    break;
                }

            }
        }
        private void Recive(object obj)
        {
            Socket socket = (Socket)(obj);
            // while (true)
            // {
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
                        if (_recvBuilder.Count == total)
                        { break; }
                    }
                    DataEventArgs e = DataEventArgs.Parse(_recvBuilder);
                    Console.WriteLine(e.ActionCmd + e.ActionParam);
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
            // }


        }
        public override void Stop()
        {
            _server.Dispose();
            _log.Error("服务退出");
        }

    }

}
