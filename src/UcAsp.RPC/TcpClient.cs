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
namespace UcAsp.RPC
{
    public class TcpClient : IClient, IDisposable
    {

        private const int buffersize = 1024 * 10;
        bool _isconnect = false;

        public Queue<Socket> DicClient { get; set; }
        public string LastError
        {
            get;

            set;
        }

        public DataEventArgs CallServiceMethod(DataEventArgs e)
        {
            // Task<DataEventArgs> task = new Task<DataEventArgs>(Call, e);
            // task.Start();
            DataEventArgs data = Call(e);
            return data;
            //return Call(e);
        }

        private DataEventArgs Call(object obj)
        {
            Socket _client;
            DataEventArgs e = (DataEventArgs)obj;
            e.CallHashCode = Convert.ToInt32(Task.CurrentId);
            int times = 0;
            while (DicClient.Count < 1)
            {
                Thread.Sleep(100);
                times++;
                Console.WriteLine("发现服务器");
                if (times > 300)
                {
                    e.ActionCmd = CallActionCmd.Timeout.ToString();
                    return e;
                }
            }

            // lock (DicClient)
            // {
            try
            {
                int trytimes = 0;
                lock (DicClient)
                {
                    while (DicClient.Peek() == null)
                    {
                        if (DicClient.Count > 0)
                        {
                            DicClient.Dequeue();
                        }
                        trytimes++;
                        Thread.Sleep(1);

                    }
                    _client = DicClient.Dequeue();
                    while (!_client.Connected)
                    {
                        if (DicClient.Count <= 0)
                        {
                            throw new Exception("当前服务器都已经掉线。");
                        }
                        _client = DicClient.Dequeue();

                    }
                }
                DicClient.Enqueue(_client);
                byte[] _bf = e.ToByteArray();
                byte[] gizpbytes = null;
                using (MemoryStream cms = new MemoryStream())
                {
                    using (System.IO.Compression.GZipStream gzip = new System.IO.Compression.GZipStream(cms, System.IO.Compression.CompressionMode.Compress))
                    {
                        //将数据写入基础流，同时会被压缩
                        gzip.Write(_bf, 0, _bf.Length);
                    }
                    gizpbytes = cms.ToArray();
                }

                _client.Send(gizpbytes, 0, gizpbytes.Length, SocketFlags.None);

                // _client.ReceiveTimeout = 900000;
                ByteBuilder _recvBuilder = new ByteBuilder(buffersize);
                if (_client.Connected)
                {
                    _client.ReceiveTimeout = 180000;
                    byte[] buffer = new byte[buffersize];
                    int total = 0;
                    while (true)
                    {

                        int len = _client.ReceiveBufferSize;
                        buffer = new byte[len];
                        _client.Receive(buffer);
                        byte[] gbuffer = null;
                        using (MemoryStream dms = new MemoryStream())
                        {
                            using (MemoryStream cms = new MemoryStream(buffer))
                            {

                                using (System.IO.Compression.GZipStream gzip = new System.IO.Compression.GZipStream(cms, System.IO.Compression.CompressionMode.Decompress))
                                {

                                    byte[] bytes = new byte[1024];
                                    int glen = 0;
                                    try
                                    {
                                        //读取压缩流，同时会被解压
                                        while ((glen = gzip.Read(bytes, 0, bytes.Length)) > 0)
                                        {
                                            dms.Write(bytes, 0, glen);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex);
                                    }
                                    gbuffer = dms.ToArray();

                                }
                            }

                        }

                        _recvBuilder.Add(gbuffer);
                        total = _recvBuilder.GetInt32(0);
                        //Thread.Sleep(1);
                        if (_recvBuilder.Count == total)
                        { break; }
                    }
                }

                _isconnect = true;
                DataEventArgs dex = DataEventArgs.Parse(_recvBuilder);
                _client.ReceiveTimeout = 99999999;
                return dex;
            }
            catch (Exception ex)
            {
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

            try
            {
                if (DicClient == null)
                { DicClient = new Queue<Socket>(); }
                for (int i = 0; i < pool; i++)
                {
                    IPEndPoint ep = new IPEndPoint(IPAddress.Parse(ip), port);
                    Socket _client = TcpConnect(ep);
                    DicClient.Enqueue(_client);
                    _isconnect = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                _isconnect = false;
            }

        }
        private Socket TcpConnect(object obj)
        {
            IPEndPoint ep = (IPEndPoint)obj;
            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client.Connect(ep);
            return client;

        }
        public bool IsConnect
        {
            get { return this._isconnect; }
        }
        public void Exit()
        {
            Dispose();
        }

        #region IDisposable Support
        private bool disposedValue = false; // 要检测冗余调用

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    int count = DicClient.Count();
                    for (int i = 0; i < count; i++)
                    {
                        Socket socket = DicClient.Dequeue();
                        socket.Disconnect(true);
                        socket.Dispose();
                    }
                }

                // TODO: 释放未托管的资源(未托管的对象)并在以下内容中替代终结器。
                // TODO: 将大型字段设置为 null。

                disposedValue = true;
            }
        }

        // TODO: 仅当以上 Dispose(bool disposing) 拥有用于释放未托管资源的代码时才替代终结器。
        // ~TcpClient() {
        //   // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
        //   Dispose(false);
        // }

        // 添加此代码以正确实现可处置模式。
        public void Dispose()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(true);
            // TODO: 如果在以上内容中替代了终结器，则取消注释以下行。
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
