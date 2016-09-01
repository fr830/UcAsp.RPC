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
        public List<IPEndPoint> IpAddress { get; set; }

        public string LastError
        {
            get;

            set;
        }

        public DataEventArgs CallServiceMethod(DataEventArgs e)
        {
            _task.Enqueue(e);
            DataEventArgs eq;
            Task<DataEventArgs> task = new Task<DataEventArgs>(() => {
                bool result = _task.TryDequeue(out eq);
                if (result)
                {
                    _runtask.Enqueue(eq);
                    DataEventArgs data = Call(eq);
                    return data;
                }
                else
                {
                    DataEventArgs data = new DataEventArgs() { ActionCmd = CallActionCmd.Error.ToString() };
                    return data;
                }

            });
            task.Start();
            return task.Result;
        }

        private DataEventArgs Call(object obj)
        {
            DataEventArgs e = (DataEventArgs)obj;
            try
            {
                Socket _client = TcpConnect(IpAddress);

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
                DataEventArgs eq;
                bool result = _task.TryDequeue(out eq);
                return dex;
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                Console.WriteLine(ex);
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
            try
            {
                List<IPEndPoint> list = (List<IPEndPoint>)obj;

                int rad = new Random().Next(list.GetHashCode()) % list.Count;               
                Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                client.Connect(list[rad]);

                return client;
            }
            catch (Exception ex)
            {
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
