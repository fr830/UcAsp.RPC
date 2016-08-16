using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UcAsp.RPC;
using TcpClient = UcAsp.RPC.TcpClient;
using System.Net;
using System.Net.Sockets;
using UcAsp.RPC;
namespace UcAsp.RPC
{
    public class ApplicationContext
    {
        private static IServer _server = null;

        private static Dictionary<string, Type> _obj = new Dictionary<string, Type>();
        private static Dictionary<string, Tuple<string, MethodInfo>> memberinfos = new Dictionary<string, Tuple<string, MethodInfo>>();
        private static Dictionary<string, Tuple<string,IClient>> proxobj = new Dictionary<string, Tuple<string, IClient>>();




        private bool _started;
        /// <summary>
        /// 客户端获取创建对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="className"></param>
        /// <returns></returns>
        public T GetProxyObject<T>()
        {
            Type sssembly = typeof(T);
            string name = sssembly.FullName;
            if (proxobj.ContainsKey(name))
            {
                string[] content = proxobj[name].Item1.Split(',');
                string nameSpace = content[0];
                string className = content[1];
                object clazz = Proxy.GetObjectType<T>(nameSpace, className);
                Type type = clazz.GetType();

                PropertyInfo property = type.GetProperty("Client");
                if (property != null && property.CanWrite)
                {
                    property.SetValue(clazz, proxobj[name].Item2, null);
                }
                return (T)clazz;
            }
            else
            {
                throw new Exception("未配置" + name);

            }
        }
        public ApplicationContext()
        {
            Config config = new Config("Application.config");
            config.GroupName = "service";
            object server = config.GetValue("server", "port");

            if (server != null)
            {
                InitializeServer(config);
            }
            config.GroupName = "client";
            object client = config.GetValue("server", "port");
            if (client != null)
            {
                InitializeClient(config);
            }
        }

        private void InitializeClient(Config config)
        {

            int count = config.GetGroupCount();
            for (int i = 0; i < count; i++)
            {
                IClient _client = new TcpClient();
                string seripaddress = (string)config.GetValue(i, "server", "ip");
                int port = Convert.ToInt32(config.GetValue(i, "server", "port"));
                int pool = Convert.ToInt32(config.GetValue(i, "server", "pool"));
                //if (_client == null || !_client.IsConnect)
                // {

                _client.Connect(seripaddress, port, pool);
                //}
                string[] assemblys = config.GetEntryNames("assmebly");
                foreach (var assname in assemblys)
                {
                    lock (proxobj)
                    {
                        object obj = config.GetValue(i, "assmebly", assname);
                        if (obj != null)
                        {
                            string ass = (string)obj.ToString();
                            if (!proxobj.ContainsKey(assname))
                            {
                                Tuple<string, IClient> tuple = new Tuple<string, IClient>(ass,_client);
                                proxobj.Add(assname, tuple);
                            }
                        }
                    }
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
                //Module[] mode = assmebly.GetModules();
                foreach (Type t in type)
                {
                    //添加类；
                    string action = t.Namespace + "." + t.Name;
                    if (!_obj.ContainsKey(action))
                    {
                        _obj.Add(action, t);
                    }
                    ///添加方法
                    MethodInfo[] infos = t.GetMethods();
                    foreach (MethodInfo info in infos)
                    {
                        string method = Proxy.GetMethodMd5Code(info);

                        Console.WriteLine(method + "." + info.Name);
                        if (!memberinfos.ContainsKey(method))
                        {
                            Tuple<string, MethodInfo> tuple = new Tuple<string, MethodInfo>(action, info);
                            memberinfos.Add(method, tuple);
                        }
                    }
                }
            }



            int port = config.GetValue("server", "port", 9008);
            _server = new TcpServer();
            _server.MemberInfos = memberinfos;
            //_tcpServer.OnReceive += Server_OnReceive;
            _server.StartListen(port);

        }



        private void Server_OnReceive(object sender, DataEventArgs e)
        {

            //Socket client = (Socket)sender;

            //int p = e.ActionParam.LastIndexOf(".");
            //string code = e.ActionParam.Substring(p + 1);

            //string name = memberinfos[code].Item1;

            //MethodInfo method = memberinfos[code].Item2;
            //var parameters = this._serializer.ToEntity<List<object>>(e.Binary);
            //if (parameters == null) parameters = new List<object>();
            //parameters = this.CorrectParameters(method, parameters);

            //Object bll = this.GetObject(name);

            //var result = method.Invoke(bll, parameters.ToArray());
            //if (!method.ReturnType.Equals(typeof(void)))
            //{
            //    e.Binary = this._serializer.ToBinary(result);
            //}
            //else
            //{
            //    e.Binary = null;
            //}
            //e.ActionCmd = CallActionCmd.Success.ToString();
            //client.Send(e.ToByteArray());

        }


        public static object GetObject(string className)
        {
            if (_obj.ContainsKey(className))
            {
                Type type = _obj[className];
                Assembly assembly = type.Assembly;
                object obj = Activator.CreateInstance(type);
                return obj;
            }
            return null;
        }
        /// <summary>
        /// 纠正参数的值
        /// Json序列化为List(object)后,object的类型和参数的类型不一致
        /// </summary>
        /// <param name="method">欲调用的目标方法</param>
        /// <param name="parameterValues">传递的参数值</param>
        /// <returns></returns>

    }
}
