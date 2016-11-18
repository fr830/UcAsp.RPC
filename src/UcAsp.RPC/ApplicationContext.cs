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
        private static ServerBase _server = null;
        private static ServerBase _httpserver = null;
        public static List<IClient> _clients = null;
        private static string _rootpath = string.Empty;
        private static ISerializer _serializer = new JsonSerializer();
        public static int _taskId = 0;

        private static Dictionary<string, Type> _obj = new Dictionary<string, Type>();
        private static Dictionary<string, Tuple<string, MethodInfo>> _memberinfos = new Dictionary<string, Tuple<string, MethodInfo>>();
        private static Dictionary<string, Tuple<string, List<IClient>>> _proxobj = new Dictionary<string, Tuple<string, List<IClient>>>();
        private static List<RegisterInfo> _registerInfo = new List<RegisterInfo>();
        private static string _config;

        private Timer Pong;//= new Timer();
        private Timer Broad;// = new Timer();
        Thread getBoardthread;
        Socket clientBoard;
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

                PropertyInfo property = type.GetProperty("Clients");
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
                            if (_p.IpPoint.Port == ep.Port && _p.IpPoint.Address.Equals(ep.Address))
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="configpath">config 绝对路径</param>
        public ApplicationContext(string configpath)
        {
            _config = configpath;
            _rootpath = AppDomain.CurrentDomain.BaseDirectory;
            Start(_rootpath + _config, _rootpath);
        }
        public ApplicationContext(string configpath, string rootpath)
        {
            _rootpath = rootpath;
            _config = configpath;
            Start(_rootpath + _config, _rootpath);

        }


        public ApplicationContext()
        {
            _rootpath = AppDomain.CurrentDomain.BaseDirectory;
            _config = _rootpath + "Application.config";
            Start(_rootpath + _config, _rootpath);
        }
        private void Start(string configpath, string rootpath)
        {
            Config config = new Config(_config) { GroupName = "service" };

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
            _log.Error("系统初始");
        }

        private void InitializeClient(Config config)
        {
            Pong = new Timer();
            string[] ipport = ((string)config.GetValue("server", "ip")).Split(';');
            int pool = Convert.ToInt32(config.GetValue("server", "pool", 2));
            for (int i = 0; i < ipport.Length; i++)
            {
                if (ipport[i].Split(':').Length > 1)
                {
                    string[] ip = ipport[i].Split(':');
                    AddClient(ip[0], int.Parse(ip[1]), pool);
                }
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

            Pong.Interval = 180000;
            Pong.Elapsed -= Pong_Elapsed;
            Pong.Elapsed += Pong_Elapsed;
            Pong.Start();
            getBoardthread = new Thread(new ThreadStart(GetBorad));

            getBoardthread.Start();

        }

        private void Pong_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (getBoardthread.IsAlive)
            {
                clientBoard.Dispose();
                clientBoard = null;
                getBoardthread.Abort();
            }
            else
            {
                getBoardthread = new Thread(new ThreadStart(GetBorad));
                getBoardthread.Start();
            }
            foreach (IClient iclient in _clients)
            {
                try
                {

                    DataEventArgs callping = new DataEventArgs() { ActionCmd = CallActionCmd.Ping.ToString(), ActionParam = "PING" };
                    DataEventArgs ping = iclient.CallServiceMethod(callping);
                    string result = _serializer.ToEntity<string>(ping.Binary);
                    Console.WriteLine(result);
                    _log.Info(result);

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
            Broad = new Timer();
            string[] assemblys = config.GetEntryNames("assmebly");
            foreach (var assname in assemblys)
            {
                string obj = config.GetValue("assmebly", assname).ToString();
                Assembly assmebly = Assembly.LoadFrom(_rootpath + obj);
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
                                //md5格式
                                Tuple<string, MethodInfo> tuple = new Tuple<string, MethodInfo>(action, info);
                                //方法类 重新方法无法实现
                                /* Tuple<string, MethodInfo> tuplepath = new Tuple<string, MethodInfo>(action, info);
                                 string path = t.Namespace + "/" + t.Name + "/" + info.Name;
                                 if (!_memberinfos.ContainsKey(path))
                                 {
                                     _memberinfos.Add(path, tuplepath);
                                 }
                                 */
                                _memberinfos.Add(method, tuple);
                            }
                        }
                    }
                }
            }



            int port = config.GetValue("server", "port", 9008);
            string protocol = (string)config.GetValue("server", "protocol");
            _server = new TcpServer();
            _httpserver = new HttpServer();

            _server.MemberInfos = _httpserver.MemberInfos = _memberinfos;
            _server.RegisterInfo = _httpserver.RegisterInfo = _registerInfo;
            //_tcpServer.OnReceive += Server_OnReceive;
            _server.StartListen(port);
            _httpserver.StartListen(port + 1);
            Broad.Interval = 30000;
            Broad.Elapsed -= Broad_Elapsed;
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
            if (!_server.IsStart)
            {
                InitializeServer(config);
                _log.Info("重启服务器");
            }
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
                if (clientBoard != null)
                    return;

                clientBoard = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                clientBoard.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, false);
                clientBoard.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                clientBoard.Bind(new IPEndPoint(IPAddress.Any, 7788));
                EndPoint endpoint = new IPEndPoint(IPAddress.Any, 0);
                while (true)
                {

                    byte[] buf = new byte[1024];
                    int l = clientBoard.ReceiveFrom(buf, ref endpoint);
                    int port = int.Parse(Encoding.Default.GetString(buf, 0, l));
                    IPAddress ip = ((IPEndPoint)endpoint).Address;
                    bool flag = false;
                    foreach (TcpClient tp in _clients)
                    {
                        foreach (ChannelPool cp in tp.IpAddress)
                        {
                            if (cp.IpPoint.Address.ToString() == ip.ToString() && cp.IpPoint.Port == port)
                            {
                                flag = true;
                                continue;
                            }

                        }

                    }
                    if (!flag)
                    {
                        AddClient(ip.ToString(), port, 10);
                    }


                }

            }
            catch (Exception ex)
            {
                // _log.Error(ex);
            }
        }

        private void AddClient(string ip, int port, int pool)
        {
            IClient _client = new TcpClient() { };
            if (_clients == null)
            {
                _clients = new List<IClient>();
            }
            bool iccon = false;

            foreach (TcpClient ic in _clients)
            {
                if (ic.IpAddress == null || ic.IpAddress.Count <= 0)
                    continue;
                if (ic.IpAddress[0].IpPoint.Address.ToString() == ip && ic.IpAddress[0].IpPoint.Port == port)
                {
                    iccon = true;
                    _client = ic;
                    break;
                }
            }
            if (!iccon)
            {

                _client.Connect(ip, port, pool);
                _clients.Add(_client);
            }

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
                            List<IClient> _listIClient = new List<IClient>();
                            _listIClient.Add(_client);
                            Tuple<string, List<IClient>> tuple = new Tuple<string, List<IClient>>(ass, _listIClient);
                            _proxobj.Add(assname, tuple);
                        }
                        else
                        {
                            List<IClient> client = _proxobj[assname].Item2;
                            client.Add(_client);
                            Tuple<string, List<IClient>> tuple = new Tuple<string, List<IClient>>(_proxobj[assname].Item1, client);
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

        public void Dispose()
        {
            _log.Info("停止 服务");
            if (clientBoard != null)
            {
                try
                {
                    clientBoard.Dispose();
                }
                catch { }
            }
            if (getBoardthread != null && getBoardthread.IsAlive)
            {
                try
                {
                    getBoardthread.Abort();
                }
                catch { }
            }
            if (_clients != null)
            {
                foreach (IClient client in _clients)
                {
                    client.Exit();
                }
            }
            if (_server != null)
            {
                _server.Stop();
            }
            if (Pong != null)
            {
                Pong.Stop();
                Pong.Dispose();
            }
            if (Broad != null)
            {
                Broad.Stop();
                Broad.Dispose();
            }

        }
    }
}
