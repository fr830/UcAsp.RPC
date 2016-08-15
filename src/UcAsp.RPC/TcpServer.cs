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
namespace UcAsp.RPC
{
    public class TcpServer : IServer
    {
        public TcpServer() { }
        private Socket _server;
        private ISerializer _serializer = new JsonSerializer();
        public Dictionary<string, Tuple<string, MethodInfo>> MemberInfos { get; set; }

       // public event EventHandler<DataEventArgs> OnReceive;
        public void StartListen(int port)
        {
            _server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this._server.Bind(new IPEndPoint(IPAddress.Any, port));
            this._server.Listen(3000);
            Console.WriteLine(_server.LocalEndPoint);

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
                        Console.WriteLine(socket.RemoteEndPoint + "/" + Thread.CurrentThread.ManagedThreadId);
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
                        Console.WriteLine(ex);
                        socket.Dispose();
                        Thread thread = Thread.CurrentThread;
                        thread.Abort();
                        

                    }
                }
            }


        }

        private List<object> CorrectParameters(MethodInfo method, List<object> parameterValues)
        {
            if (parameterValues.Count == method.GetParameters().Length)
            {
                for (int i = 0; i < parameterValues.Count; i++)
                {
                    // 传递的参数值
                    object entity = parameterValues[i];
                    // 传递参数的类型
                    Type eType = entity.GetType();

                    Type[] ParameterTypes = new Type[method.GetParameters().Length];
                    for (int x = 0; x < method.GetParameters().Length; x++)
                    {
                        ParameterTypes[x] = method.GetParameters()[x].ParameterType;
                    }
                    // 目标方法参数类型
                    Type pType = ParameterTypes[i];
                    // 类型不一致，需要转换类型
                    if (eType.Equals(pType) == false)
                    {
                        // 转换entity的类型
                        Binary bin = this._serializer.ToBinary(entity);
                        object pValue = this._serializer.ToEntity(bin, pType);
                        // 保存参数
                        parameterValues[i] = pValue;
                    }
                }
            }

            return parameterValues;
        }


        private void Call(Socket socket, DataEventArgs e)
        {
            int p = e.ActionParam.LastIndexOf(".");

            string code = e.ActionParam.Substring(p + 1);

            string name = MemberInfos[code].Item1;

            MethodInfo method = MemberInfos[code].Item2;
            var parameters = this._serializer.ToEntity<List<object>>(e.Binary);
            if (parameters == null) parameters = new List<object>();
            parameters = this.CorrectParameters(method, parameters);

            Object bll = ApplicationContext.GetObject(name);

            var result = method.Invoke(bll, parameters.ToArray());
            if (!method.ReturnType.Equals(typeof(void)))
            {
                e.Binary = this._serializer.ToBinary(result);
            }
            else
            {
                e.Binary = null;
            }
            e.ActionCmd = CallActionCmd.Success.ToString();
            socket.Send(e.ToByteArray());
        }
    }
}
