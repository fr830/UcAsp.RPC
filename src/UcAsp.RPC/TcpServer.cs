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
        private Dictionary<string, Socket> connection = new Dictionary<string, Socket>();

        private CancellationTokenSource token = new CancellationTokenSource();
        public override void StartListen(int port)
        {
            this.IsStart = true;
            _server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, true);
            this._server.Bind(new IPEndPoint(IPAddress.Any, port));
            this._server.Listen(3000);
            _log.Info("开启服务：" + port);
            Thread thread = new Thread(new ParameterizedThreadStart(Start));
            thread.Start(null);
        }
        #region  同步

        private void Start(object obj)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    Socket client = _server.Accept();
                    connection.Add(client.RemoteEndPoint.ToString(), client);
                    Console.WriteLine(client.RemoteEndPoint.ToString());
                    ThreadPool.QueueUserWorkItem(Accept, client);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    _log.Error(ex);
                }

            }
        }
        private void Accept(object socket)
        {
            Socket client = (Socket)socket;
            StateObject state = new StateObject();
            try
            {
                state.WorkSocket = client;
                client.BeginReceive(state.Buffer, 0, StateObject.BufferSize, SocketFlags.None, ReceiveCallback, state);
            }
            catch (Exception ex)
            {
                GC.Collect();
                _log.Error(ex);
                Console.WriteLine(ex);
                IsStart = false;
                try
                {
                    state.Builder.ReSet();
                    _server.Disconnect(true);
                    _server.Dispose();
                }
                catch (Exception e)
                {
                    _log.Error(e);
                    Console.WriteLine(e);
                }
            }
        }
        #endregion
        public override void Stop()
        {
            IsStart = false;
            try
            {

                _server.Close();
                _server.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                _log.Error(ex);
            }
            foreach (KeyValuePair<string, Socket> kv in connection)
            {
                try
                {
                    kv.Value.Shutdown(SocketShutdown.Both); kv.Value.BeginDisconnect(true, null, null); kv.Value.Close(); kv.Value.Dispose();

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    Console.WriteLine(ex);
                }
            }

            try
            {
                token.Cancel();
                for (int i = 0; i < Environment.ProcessorCount * 2; i++)
                {
                    _startThread[i].Abort();

                }
            }
            catch (Exception ex)
            { _log.Error(ex); }
            GC.Collect();
            GC.SuppressFinalize(this);
            Console.WriteLine("服务退出");
            _log.Error("服务退出");
        }
        private void ReceiveCallback(IAsyncResult result)
        {
            StateObject state = (StateObject)result.AsyncState;
            Socket handler = state.WorkSocket;
            try
            {


                int bytesRead = handler.EndReceive(result);
                if (bytesRead > 0)
                {
                    state.Builder.Add(state.Buffer, 0, bytesRead);
                    int total = state.Builder.GetInt32(0);

                    if (total == state.Builder.Count)
                    {
                        DataEventArgs dex = DataEventArgs.Parse(state.Builder);
                        Call(handler, dex);
                        Console.WriteLine(dex.TaskId);
                        _log.Error("执行任务：" + dex.TaskId);
                        state.Builder.ReSet();
                        state.WorkSocket = handler;
                        handler.BeginReceive(state.Buffer, 0, StateObject.BufferSize, SocketFlags.None, ReceiveCallback, state);
                    }
                    else
                    {
                        handler.BeginReceive(state.Buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                GC.Collect();
                state.Builder.ReSet();
                _log.Error(ex);
                try
                {
                    handler.Dispose();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    _log.Error(e);
                }
            }
        }

    }

}
