using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using log4net;
using System.Text;
using System.Net.Sockets;
using System.Net;
using Timer = System.Timers.Timer;
using System.Threading;
using System.Threading.Tasks;
namespace UcAsp.RPC
{
    public class ApplicationContext
    {
        private readonly ILog _log = LogManager.GetLogger(typeof(ApplicationContext));
        private static IServer _server = null;
        private static IServer _httpserver = null;
        public static List<IClient> _clients = null;

        private static ISerializer _serializer = new JsonSerializer();

        private static Dictionary<string, Type> _obj = new Dictionary<string, Type>();
        private static Dictionary<string, Tuple<string, MethodInfo>> _memberinfos = new Dictionary<string, Tuple<string, MethodInfo>>();
        private static Dictionary<string, Tuple<string, IClient>> _proxobj = new Dictionary<string, Tuple<string, IClient>>();
        private static List<RegisterInfo> _registerInfo = new List<RegisterInfo>();
        private static string _config;

        private Timer Pong = new Timer();
        private Timer Broad = new Timer();

        /// <summary>
        /// 客户端获取创建对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="className"></param>
        /// <returns></returns>
        public T GetProxyObject<T>()
        {
            Type asssembly = typeof(T);
            string name = asssembly.FullName;
            if (_proxobj.ContainsKey(name))
            {
                string[] content = _proxobj[name].Item1.Split(',');
                string nameSpace = content[0];
                string className = content[1];
                object clazz = Proxy.GetObjectType<T>(nameSpace, className);
                Type type = clazz.GetType();

                PropertyInfo property = type.GetProperty("Client");
                if (property != null && property.CanWrite)
                {
                    property.SetValue(clazz, _proxobj[name].Item2, null);
                }
                return (T)clazz;
            }
            else
            {
                _log.Error("未配置" + name);
                throw new Exception("未配置" + name);

            }
        }

        public T GetProxyObject<T>(IPEndPoint ep)
        {
            IClient client = null;
            Type asssembly = typeof(T);
            string name = asssembly.FullName;
            if (_proxobj.ContainsKey(name))
            {
                string[] content = _proxobj[name].Item1.Split(',');
                string nameSpace = content[0];
                string className = content[1];
                object clazz = Proxy.GetObjectType<T>(nameSpace, className);
                Type type = clazz.GetType();
                PropertyInfo property = type.GetProperty("Client");
                if (property != null && property.CanWrite)
                {
                    foreach (IClient _c in _clients)
                    {
                        foreach (ChannelPool _p in _c.IpAddress)
                        {
                            if (_p.IpAddress.Port == ep.Port && _p.IpAddress.Address.Equals(ep.Address))
                                break;
                        }
                        client = _c;
                    }

                    property.SetValue(clazz, client, null);
                }
                return (T)clazz;
            }
            else
            {
                _log.Error("未配置" + name);
                throw new Exception("未配置" + name);

            }
        }
        public ApplicationContext(string configpath)
        {
            _config = configpath;
            Config config = new Config(configpath) { GroupName = "service" };

            object server = config.GetValue("server", "port");
            if (server != null)
            {
                InitializeServer(config);
            }
            config.GroupName = "client";
            object client = config.GetValue("server", "ip");
            if (client != null)
            {
                InitializeClient(config);
            }
        }



        public ApplicationContext()
        {
            _config = "Application.config";
            new ApplicationContext("Application.config");
        }

        private void InitializeClient(Config config)
        {

            string[] ipport = ((string)config.GetValue("server", "ip")).Split(';');
            int pool = Convert.ToInt32(config.GetValue("server", "pool"));
            for (int i = 0; i < ipport.Length; i++)
            {
                string[] ip = ipport[i].Split(':');
                AddClient(ip[0], int.Parse(ip[1]));
            }
            string[] relation = config.GetEntryNames("relation");
            if (relation != null)
            {
                Proxy.RelationDll = new Dictionary<string, string>();
                foreach (string va in relation)
                {

                    if (!Proxy.RelationDll.ContainsKey(va))
                    {
                        object rdll = config.GetValue("relation", va);
                        if (rdll != null)
                        {
                            Proxy.RelationDll.Add(va, rdll.ToString());
                        }

                    }
                }
            }

            Pong.Interval = 30000;
            Pong.Elapsed += Pong_Elapsed;
            Pong.Start();
            Thread thread = new Thread(new ThreadStart(GetBorad));

            thread.Start();

        }

        private void Pong_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            foreach (IClient iclient in _clients)
            {
                try
                {

                    DataEventArgs callping = new DataEventArgs() { ActionCmd = CallActionCmd.Ping.ToString(), ActionParam = "PING" };
                    DataEventArgs ping = iclient.CallServiceMethod(callping);
                    string result = _serializer.ToEntity<string>(ping.Binary);
                    Console.WriteLine(result);

                }
                catch (Exception ex)
                {
                    try
                    {
                        Config config = new Config(_config) { GroupName = "service" };
                        config.GroupName = "client";
                        InitializeClient(config);
                    }
                    catch (Exception e0) { _log.Error(e0); }
                    _log.Error(ex);
                }

            }

        }
        private void InitializeServer(Config config)
        {
            string[] assemblys = config.GetEntryNames("assmebly");
            foreach (var assname in assemblys)
            {
                string obj = config.GetValue("assmebly", assname).ToString();
                Assembly assmebly = Assembly.LoadFrom(obj);
                Type[] type = assmebly.GetTypes();
                foreach (Type t in type)
                {
                    Type[] _interface = t.GetInterfaces();
                    if (_interface.Length > 0)
                    {
                        //添加类；
                        string action = string.Format("{0}.{1}", t.Namespace, t.Name);
                        if (!_obj.ContainsKey(action))
                        {
                            _obj.Add(action, t);
                        }


                        RegisterInfo reg = new RegisterInfo() { ClassName = t.Name, NameSpace = t.Namespace, FaceNameSpace = _interface[0].Namespace, InterfaceName = _interface[0].Name };
                        _registerInfo.Add(reg);
                        ///添加方法
                        MethodInfo[] infos = t.GetMethods();
                        foreach (MethodInfo info in infos)
                        {
                            string method = Proxy.GetMethodMd5Code(info);
                            _log.Info(string.Format("{0}.{1}", method, info.Name));
                            if (!_memberinfos.ContainsKey(method))
                            {
                                Tuple<string, MethodInfo> tuple = new Tuple<string, MethodInfo>(action, info);
                                _memberinfos.Add(method, tuple);
                            }
                        }
                    }
                }
            }



            int port = config.GetValue("server", "port", 9008);
            string protocol = (string)config.GetValue("server", "protocol");
            // if (protocol.Equals("tcp"))
            //{
            _server = new TcpServer();
            _httpserver = new HttpServer();
            // }

            _server.MemberInfos = _httpserver.MemberInfos = _memberinfos;
            _server.RegisterInfo = _httpserver.RegisterInfo = _registerInfo;
            //_tcpServer.OnReceive += Server_OnReceive;
            _server.StartListen(port);
            _httpserver.StartListen(port + 1);
            Broad.Interval = 10000;
            Broad.Elapsed += Broad_Elapsed;
            Broad.Start();

        }

        private void Broad_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Broadcast();
        }

        /// <summary>
        /// 广播自己的端口
        /// </summary>
        private void Broadcast()
        {

            Config config = new Config(_config) { GroupName = "service" };
            int port = config.GetValue("server", "port", 9008);
            using (UdpClient client = new UdpClient(new IPEndPoint(IPAddress.Any, 0)))
            {
                IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse("255.255.255.255"), 7788);
                byte[] buf = Encoding.Default.GetBytes(port.ToString());
                client.Send(buf, buf.Length, endpoint);
            }

        }
        /// <summary>
        /// 接收广播添加服务
        /// </summary>
        private void GetBorad()
        {
            try
            {
                // UdpClient client = new UdpClient(new IPEndPoint(IPAddress.Any, 7788));
                Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, false);
                client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                client.Bind(new IPEndPoint(IPAddress.Any, 7788));
                EndPoint endpoint = new IPEndPoint(IPAddress.Any, 0);
                while (true)
                {
                    byte[] buf = new byte[1024];
                    int l = client.ReceiveFrom(buf, ref endpoint);
                    int port = int.Parse(Encoding.Default.GetString(buf, 0, l));
                    IPAddress ip = ((IPEndPoint)endpoint).Address;
                    bool flag = false;
                    foreach (TcpClient tp in _clients)
                    {
                        foreach (ChannelPool cp in tp.IpAddress)
                        {
                            if (cp.IpAddress.Address.ToString() == ip.ToString() && cp.IpAddress.Port == port)
                            {
                                flag = true;
                                continue;
                            }

                        }

                    }
                    if (!flag)
                    {
                        AddClient(ip.ToString(), port);
                    }
                }

            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
            finally
            {

            }
        }

        ~ApplicationContext()
        {

            if (Pong != null)
            {
                Pong.Dispose();
            }
        }

        //private void Server_OnReceive(object sender, DataEventArgs e)
        //{

        //    //Socket client = (Socket)sender;

        //    //int p = e.ActionParam.LastIndexOf(".");
        //    //string code = e.ActionParam.Substring(p + 1);

        //    //string name = memberinfos[code].Item1;

        //    //MethodInfo method = memberinfos[code].Item2;
        //    //var parameters = this._serializer.ToEntity<List<object>>(e.Binary);
        //    //if (parameters == null) parameters = new List<object>();
        //    //parameters = this.CorrectParameters(method, parameters);

        //    //Object bll = this.GetObject(name);

        //    //var result = method.Invoke(bll, parameters.ToArray());
        //    //if (!method.ReturnType.Equals(typeof(void)))
        //    //{
        //    //    e.Binary = this._serializer.ToBinary(result);
        //    //}
        //    //else
        //    //{
        //    //    e.Binary = null;
        //    //}
        //    //e.ActionCmd = CallActionCmd.Success.ToString();
        //    //client.Send(e.ToByteArray());

        //}


        private void AddClient(string ip, int port)
        {
            if (_clients == null)
            {
                _clients = new List<IClient>();
            }

            TcpClient _client = new TcpClient() { IpAddress = new List<ChannelPool>() };
            IPEndPoint ep = new IPEndPoint(IPAddress.Parse(ip), port);
            ChannelPool cpl = new ChannelPool() { Available = true, IpAddress = ep, PingActives = DateTime.Now.Ticks, RunTimes = 0 };
            _client.IpAddress.Add(cpl);
            // _client.IpAddress = new List<IPEndPoint>();



            _clients.Add(_client);


            DataEventArgs callreg = new DataEventArgs() { ActionCmd = CallActionCmd.Register.ToString(), ActionParam = "Register" };
            DataEventArgs reg = _client.CallServiceMethod(callreg);
            List<RegisterInfo> registerInfos = _serializer.ToEntity<List<RegisterInfo>>(reg.Binary);
            if (registerInfos != null)
            {
                foreach (RegisterInfo info in registerInfos)
                {
                    lock (_proxobj)
                    {
                        string assname = string.Format("{0}.{1}", info.FaceNameSpace, info.InterfaceName);
                        string ass = string.Format("{0},{1}", info.NameSpace, info.ClassName);
                        if (!_proxobj.ContainsKey(assname))
                        {
                            Tuple<string, IClient> tuple = new Tuple<string, IClient>(ass, _client);
                            ChannelPool cpls = new ChannelPool() { Available = true, IpAddress = ep, PingActives = DateTime.Now.Ticks, RunTimes = 0 };

                            _client.IpAddress = new List<ChannelPool>();
                            _client.IpAddress.Add(cpls);
                            _proxobj.Add(assname, tuple);
                        }
                        else
                        {
                            IClient client = _proxobj[assname].Item2;
                            bool iscontant = false;
                            foreach (ChannelPool eip in client.IpAddress)
                            {
                                if (eip.IpAddress == ep)
                                    iscontant = true;
                            }
                            if (!iscontant)
                            {
                                ChannelPool cpls = new ChannelPool() { Available = true, IpAddress = ep, PingActives = DateTime.Now.Ticks, RunTimes = 0 };
                                client.IpAddress.Add(cpls);
                            }

                            Tuple<string, IClient> tuple = new Tuple<string, IClient>(_proxobj[assname].Item1, client);
                            _proxobj[assname] = tuple;
                        }
                    }
                }
            }
        }
        public static object GetObject(string className)
        {
            if (_obj.ContainsKey(className))
            {
                Type type = _obj[className];
                object obj = Activator.CreateInstance(type);
                return obj;
            }
            return null;
        }



    }
}
