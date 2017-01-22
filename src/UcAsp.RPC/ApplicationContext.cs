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
    [Serializable]
    public class ApplicationContext : IDisposable
    {
        private readonly ILog _log = LogManager.GetLogger(typeof(ApplicationContext));
        private static ServerBase _server = null;
        private static ServerBase _httpserver = null;
        public static IClient _clients = null;
        private static string _rootpath = string.Empty;
        private static ISerializer _serializer = new JsonSerializer();
        public static int _taskId = 0;
        private static bool _run = false;
        private static Dictionary<string, Type> _obj = new Dictionary<string, Type>();
        private static Dictionary<string, Tuple<string, MethodInfo, int>> _memberinfos = new Dictionary<string, Tuple<string, MethodInfo, int>>();
        private static Dictionary<string, Tuple<string, List<ChannelPool>>> _proxobj = new Dictionary<string, Tuple<string, List<ChannelPool>>>();
        private static List<RegisterInfo> _registerInfo = new List<RegisterInfo>();
        private static string _config;
        CancellationTokenSource cts = new CancellationTokenSource();
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
                    property.SetValue(clazz, _clients, null);
                }
                return (T)clazz;
            }
            else
            {
                _log.Error("未配置" + name);
                throw new Exception("未配置" + name);

            }
        }

        public DateTime LastRunTime()
        {
            if (_server != null)
            {
                return _server.LastRunTime;
            }
            else
            {
                return DateTime.Parse("0001/01/01 00:00");
            }
        }
        public string LastError()
        {
            if (_server != null)
            {
                return _server.LastError;
            }
            else
            {
                return "服务不存在";
            }
        }

        public string LastMethod()
        {
            if (_server != null)
            {
                return _server.LastMethod;
            }
            else
            {
                return "服务不存在";
            }

        }
        public string LastParam()
        {

            if (_server != null)
            {
                return _server.LastParam;
            }
            else
            {
                return "服务不存在";
            }
        }

        public int Timer()
        {
            if (_server != null)
            {
                return _server.Timer;
            }
            else
            {
                return 0;
            }
        }

        public T GetProxyObject<T>(IPEndPoint ep)
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
                    // foreach (IClient _c in _clients)
                    // {
                    //foreach (ChannelPool _p in _clients.IpAddress)
                    //{
                    //    if (_p.IpPoint.Port == ep.Port && _p.IpPoint.Address.Equals(ep.Address))
                    //        break;
                    //}
                    //client = _clients;
                    //  }

                    property.SetValue(clazz, _clients, null);
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
            if (!_rootpath.EndsWith("\\"))
            {
                _rootpath = _rootpath + "\\";
            }
            Start(_rootpath + _config, _rootpath);
        }
        public ApplicationContext(string config, string rootpath)
        {
            _rootpath = rootpath;
            if (!_rootpath.EndsWith("\\"))
            {
                _rootpath = _rootpath + "\\";
            }
            _config = _rootpath + config;
            Start(_config, _rootpath);

        }

        ~ApplicationContext()
        {
            Dispose();
        }
        public ApplicationContext()
        {
            _rootpath = AppDomain.CurrentDomain.BaseDirectory;
            if (!_rootpath.EndsWith("\\"))
            {
                _rootpath = _rootpath + "\\";
            }
            _config = _rootpath + "Application.config";
            // Start(_config, _rootpath);
        }
        public void Start(string configpath, string rootpath)
        {
            _config = configpath;
            _rootpath = rootpath;
            Config config = new Config(configpath) { GroupName = "service" };
            _log.Info(configpath);
            object server = config.GetValue("server", "port");
            if (server != null)
            {
                InitializeServer(config);
                _log.Error("服务器端初始成功");
            }
            config.GroupName = "client";
            object client = config.GetValue("server", "ip");
            if (client != null)
            {
                InitializeClient(config);
                _log.Error("客户端初始成功");
            }

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
                    ServerPort port = new ServerPort() { Ip = ip[0], Port = int.Parse(ip[1]), Pool = pool };
                    AddClient(port);

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

            //foreach (ChannelPool iclient in _clients.IpAddress.Where(o=>o.Available==false))
            //{
            //    try
            //    {

            //        DataEventArgs callping = new DataEventArgs() { ActionCmd = CallActionCmd.Ping.ToString(), ActionParam = "PING" };
            //        _clients.CallServiceMethod(callping);
            //        DataEventArgs ping = _clients.GetResult(callping);
            //        string result = _serializer.ToEntity<string>(ping.Binary);
            //        Console.WriteLine(result);
            //        _log.Info(result);

            //    }
            //    catch (Exception ex)
            //    {

            //    }

            //}

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
                                Tuple<string, MethodInfo, int> tuple = new Tuple<string, MethodInfo, int>(action, info, 0);
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
            string password = (string)config.GetValue("server", "password");
            string username = (string)config.GetValue("server", "username");
            _server = new TcpServer();
            _httpserver = new HttpServer();

            _server.MemberInfos = _httpserver.MemberInfos = _memberinfos;
            _server.RegisterInfo = _httpserver.RegisterInfo = _registerInfo;
            _httpserver.Authorization = _server.Authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
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

                    //byte[] buf = new byte[1024];
                    //int l = clientBoard.ReceiveFrom(buf, ref endpoint);
                    //int port = int.Parse(Encoding.Default.GetString(buf, 0, l));
                    //IPAddress ip = ((IPEndPoint)endpoint).Address;
                    //bool flag = false;

                    //foreach (ChannelPool cp in _clients.IpAddress)
                    //{
                    //    if (cp.IpPoint.Address.ToString() == ip.ToString() && cp.IpPoint.Port == port)
                    //    {
                    //        flag = true;
                    //        continue;
                    //    }

                    //}

                    //if (!flag)
                    //{
                    //    ServerPort sport = new ServerPort() { Ip = ip.ToString(), Port = port, Pool = 10 };
                    //    AddClient(sport);
                    //}


                }

            }
            catch (Exception ex)
            {
                // _log.Error(ex);
            }
        }

        private void AddClient(object serverport)
        {
            ServerPort sport = (ServerPort)serverport;
            string ip = sport.Ip;
            int port = sport.Port;
            int pool = sport.Pool + 2;
            Config config = new Config(_config) { GroupName = "client" };
            if (_clients == null)
            {
                string mode = config.GetValue("server", "mode", "tcp");
                string password = (string)config.GetValue("server", "password");
                string username = (string)config.GetValue("server", "username");
                if (mode.ToLower() == "tcp")
                {
                    _clients = new TcpClient();
                }
                else
                {
                    _clients = new HttpClient();
                }
                _clients.Authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
                _clients.ClientTask = new Queue<DataEventArgs>();
                _clients.ResultTask = new Dictionary<int, DataEventArgs>();
                _clients.RuningTask = new Dictionary<int, DataEventArgs>();
                _clients.IpAddress = new List<ChannelPool>();
            }
            bool iccon = false;
            if (_clients.IpAddress.Count <= 0)
            {
                //
                _clients.Connect(ip, port, pool);
                ///

            };



            foreach (ChannelPool ic in _clients.IpAddress)
            {

                if (ic.IpPoint.Address.ToString() == ip && ic.IpPoint.Port == port)
                {
                    iccon = true;
                    break;
                }
            }
            if (!iccon)
            {
                _clients.Connect(ip, port, pool);

            }
            if (!_run)
            {
                _clients.Run();
                _run = true;
            }
            DataEventArgs callreg = new DataEventArgs() { ActionCmd = CallActionCmd.Register.ToString(), ActionParam = "Register", T = typeof(List<RegisterInfo>) };
            callreg.CallHashCode = callreg.GetHashCode();
            _clients.CallServiceMethod(callreg);
            DataEventArgs reg = _clients.GetResult(callreg);
            if (reg.StatusCode != StatusCode.Success)
                return;
            List<RegisterInfo> registerInfos = new List<RegisterInfo>();
            if (!string.IsNullOrEmpty(reg.Json))
            {
                registerInfos = _serializer.ToEntity<List<RegisterInfo>>(reg.Json);
            }
            else
            {
                registerInfos = _serializer.ToEntity<List<RegisterInfo>>(reg.Binary);
            }
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
                            List<ChannelPool> _listIClient = new List<ChannelPool>();
                            // _listIClient.Add(channel);
                            Tuple<string, List<ChannelPool>> tuple = new Tuple<string, List<ChannelPool>>(ass, _listIClient);
                            _proxobj.Add(assname, tuple);
                        }
                        else
                        {
                            List<ChannelPool> client = _proxobj[assname].Item2;
                            //  client.Add(channel);
                            Tuple<string, List<ChannelPool>> tuple = new Tuple<string, List<ChannelPool>>(_proxobj[assname].Item1, client);
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

                _clients.Exit();

            }
            if (_server != null)
            {
                _server.Stop();
            }
            if (_httpserver != null)
            {
                _httpserver.Stop();
            }
            GC.SuppressFinalize(this);
            cts.Cancel();
            cts.Token.Register(() =>
            {
                Console.WriteLine("停止 服务");
            });

        }
    }
}
