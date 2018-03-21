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
using System.Collections.Concurrent;
using UcAsp.RPC.Service;

namespace UcAsp.RPC
{
    [Serializable]
    public class ApplicationContext : IDisposable
    {
        private readonly ILog _log = LogManager.GetLogger(typeof(ApplicationContext));
        private static ServerBase _server = null;
        private static ServerBase _httpserver = null;
        private static string _rootpath = string.Empty;
        private static ISerializer _serializer = new JsonSerializer();
        public static int _taskId = 0;
        private static bool _run = false;
        private static Dictionary<string, Type> _obj = new Dictionary<string, Type>();
        private static Dictionary<string, Tuple<string, MethodInfo, int, long>> _memberinfos = new Dictionary<string, Tuple<string, MethodInfo, int, long>>();
        private static Dictionary<string, dynamic> _proxobj = new Dictionary<string, dynamic>();
        private static ConcurrentDictionary<string, object> _clazz = new ConcurrentDictionary<string, object>();
        private static List<RegisterInfo> _registerInfo = new List<RegisterInfo>();
        private static string _config;
        CancellationTokenSource cts = new CancellationTokenSource();
        private Timer Pong;//= new Timer();
        private Timer Broad;// = new Timer();
        Thread getBoardthread;
        Socket clientBoard;

        #region 监控
        public DateTime LastRunTime()
        {
            if (_server != null)
            {
                return _server.LastRunTime;
            }
            else
            {
                return ActionService.LastRunTime;
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
                return ActionService.LastError;
            }

        }

        public string LastMethod()
        {
            //if (actionService != null)
            //{
            //    return ActionService.LastMethod;
            //}
            //else
            //{
            //    return "服务不存在";
            //}

            return ActionService.LastMethod;
        }
        public string LastParam()
        {

            if (_server != null)
            {
                return _server.LastParam;
            }
            else
            {
                return ActionService.LastParam;
            }
        }
        public string ManagerUrl()
        {
            if (_httpserver != null)
            {
                return _httpserver.ManagerUrl();
            }
            else
            {
                return null;
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
        #endregion

        /// <summary>
        /// 客户端获取创建对象
        /// </summary>
        /// <typeparam name="T">接口</typeparam>
        /// <returns></returns>
        public T GetProxyObject<T>()
        {
            Type asssembly = typeof(T);
            string name = asssembly.FullName;
            if (_proxobj.ContainsKey(name))
            {
                dynamic _client = _proxobj[name];

                string nameSpace = _client.NameSpace;
                string className = _client.ClassName;
                object clazz;
                if (!_clazz.TryGetValue(nameSpace + "." + className, out clazz))
                {
                    clazz = Proxy.GetObjectType<T>(nameSpace, className);

                    Type type = clazz.GetType();
                    PropertyInfo property = type.GetProperty("Client");
                    if (property != null && property.CanWrite)
                    {
                        property.SetValue(clazz, _client.Client, null);
                    }
                    _clazz.AddOrUpdate(nameSpace + "." + className, clazz, (key, value) => value = clazz);
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

            Type asssembly = typeof(T);
            string name = asssembly.FullName;
            if (_proxobj.ContainsKey(name))
            {
                dynamic _client = _proxobj[name];

                string nameSpace = _client.NameSpace;
                string className = _client.ClassName;
                object clazz = Proxy.GetObjectType<T>(nameSpace, className);
                Type type = clazz.GetType();
                PropertyInfo property = type.GetProperty("Client");
                if (property != null && property.CanWrite)
                {
                    property.SetValue(clazz, _client.Client, null);
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
        /// Initializes a new instance of the <see cref="ApplicationContext"/> class. 
        /// 初始化
        /// </summary>
        /// <param name="configpath">
        /// config 绝对路径
        /// </param>
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
                        Type[] tx = t.GetInterfaces();
                        MethodInfo[] infos = t.GetMethods();
                        foreach (MethodInfo info in infos)
                        {

                            if (info.DeclaringType.Name != reg.ClassName)
                                continue;
                            string method = Proxy.GetMethodMd5Code(info);
                            _log.Info(string.Format("{0}.{1}", method, info.Name));
                            if (!_memberinfos.ContainsKey(method))
                            {

                                Tuple<string, MethodInfo, int,long> tuple = new Tuple<string, MethodInfo, int,long>(action, info, 0,0);
                                _memberinfos.Add(method, tuple);
                                object[] attr = info.GetCustomAttributes(typeof(Restful), true);
                                if (attr.Length > 0)
                                {
                                    Restful rf = (Restful)attr[0];
                                    if (rf.IsRun)
                                    {
                                        object _runObj = Activator.CreateInstance(t);
                                        info.Invoke(_runObj, null);
                                    }
                                }
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

                if (clientBoard != null)
                    return;

                clientBoard = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                clientBoard.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, false);
                clientBoard.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                clientBoard.Bind(new IPEndPoint(IPAddress.Any, 7788));
                EndPoint endpoint = new IPEndPoint(IPAddress.Any, 0);
                while (!cts.Token.IsCancellationRequested)
                {

                    Dictionary<string, dynamic> dic = new Dictionary<string, dynamic>(_proxobj);
                    byte[] buf = new byte[1024];
                    int l = clientBoard.ReceiveFrom(buf, ref endpoint);
                    int port = int.Parse(Encoding.Default.GetString(buf, 0, l));
                    IPAddress ip = ((IPEndPoint)endpoint).Address;
                    bool flag = false;

                    foreach (KeyValuePair<string, dynamic> cp in dic)
                    {
                        foreach (ChannelPool p in cp.Value.Client.Channels)
                        {
                            if (p.IpPoint.Address.ToString() == ip.ToString() && p.IpPoint.Port == port)
                            {
                                flag = true;
                                break;
                            }
                        }
                        if (flag == true)
                            break;
                    }

                    if (!flag)
                    {
                        ServerPort sport = new ServerPort() { Ip = ip.ToString(), Port = port, Pool = 10 };
                        AddClient(sport);
                    }


                }


            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }

        private void AddClient(object serverport)
        {
            ServerPort sport = (ServerPort)serverport;
            string ip = sport.Ip;
            int port = sport.Port;
            int pool = sport.Pool;
            Config config = new Config(_config) { GroupName = "client" };
            IClient _client;
            string mode = config.GetValue("server", "mode", "tcp");
            string password = (string)config.GetValue("server", "password");
            string username = (string)config.GetValue("server", "username");
            if (mode.ToLower() == "tcp")
            {
                _client = new SocketClient();
            }
            else
            {
                _client = new HttpClient();
            }

            ChannelPool channlepool = new ChannelPool { IpPoint = new IPEndPoint(IPAddress.Parse(ip), port), Available = true };
            _client.Channels.Add(channlepool);
            _client.Authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
            DataEventArgs vali = new DataEventArgs { ActionCmd = CallActionCmd.Validate.ToString(), ActionParam = _client.Authorization, TaskId = 0 };
            DataEventArgs valiresult = _client.GetResult(vali, channlepool);
            if (valiresult.StatusCode != StatusCode.Success)///如果验证失败退出
                return;


            _client.Authorization = valiresult.HttpSessionId;

            DataEventArgs callreg = new DataEventArgs() { HttpSessionId = _client.Authorization, ActionCmd = CallActionCmd.Register.ToString(), ActionParam = "Register", T = typeof(List<RegisterInfo>) };


            DataEventArgs reg = _client.GetResult(callreg, channlepool);

            if (reg.StatusCode != StatusCode.Success)
                return;

            _client.Run();
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
                for (int i = 0; i < pool; i++)
                {
                    ChannelPool channel = new ChannelPool { IpPoint = new IPEndPoint(IPAddress.Parse(ip), port), Available = true };
                    _client.Channels.Add(channel);
                }
                foreach (RegisterInfo info in registerInfos)
                {

                    lock (_proxobj)
                    {
                        try
                        {
                            string assname = string.Format("{0}.{1}", info.FaceNameSpace, info.InterfaceName);

                            dynamic val = new { ClassName = info.ClassName, NameSpace = info.NameSpace, Client = _client };
                            if (!_proxobj.ContainsKey(assname))
                            {
                                _proxobj.Add(assname, val);
                            }
                            else
                            {
                                bool pc = false;
                                foreach (ChannelPool p in _proxobj[assname].Client.Channels)
                                {
                                    if (p.IpPoint == channlepool.IpPoint)
                                    {
                                        pc = true;
                                    }
                                }
                                if (!pc)
                                {
                                    _proxobj[assname].Client.Channels.Add(channlepool);
                                    for (int i = 0; i < 10; i++)
                                    {
                                        ChannelPool channel = new ChannelPool { IpPoint = new IPEndPoint(IPAddress.Parse(ip), port), Available = true };
                                        _proxobj[assname].Client.Channels.Add(channel);
                                    }
                                }

                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
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
