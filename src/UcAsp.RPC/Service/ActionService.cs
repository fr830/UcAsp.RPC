/***************************************************
*创建人:rixiang.yu
*创建时间:2017/7/15 18:25:32
*功能说明:<Function>
*版权所有:<Copyright>
*Frameworkversion:4.0
*CLR版本：4.0.30319.42000
***************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UcAsp.WebSocket.Server;
using UcAsp.WebSocket.Net;
using UcAsp.WebSocket;
using Newtonsoft.Json;
using System.IO;
using System.Reflection;
using System.Threading;
namespace UcAsp.RPC.Service
{

    public class ActionService : WebSocketBehavior
    {
        CancellationTokenSource source = new CancellationTokenSource();
        CancellationToken token;
        private Dictionary<string, DateTime> RunCall = new Dictionary<string, DateTime>();
        public Dictionary<string, Tuple<string, MethodInfo, int>> MemberInfos { get; set; }
        protected override void OnPost(HttpRequestEventArgs ev)
        {
            string content = string.Empty;
            using (StreamReader reader = new StreamReader(ev.Request.InputStream))
            {
                content = reader.ReadToEnd();
            }
            MethodInfo method = null;

            var parameters = JsonConvert.DeserializeObject<dynamic>(content);
            if (parameters == null) parameters = new List<object>();

            string rpc = ev.Request.Headers["UcAsp.Net_RPC"];
            string url = ev.Request.RawUrl;
            if (!url.EndsWith("/"))
            {
                url = url + "/";
            }
            string[] rurl = url.Split('/');
            string name = string.Empty;
            string methodname = string.Empty;
            string code = string.Empty;
            if (rurl.Length == 5)
            {
                name = rurl[2].ToLower();
                methodname = rurl[3].ToLower();
            }
            else
            {
                code = rurl[1];
            }
            bool keeplive = false;
            DataEventArgs ea = Call(name, methodname, code, parameters, ref keeplive);

            if (rpc == "true")
            {
                byte[] buffer = GZipUntil.GetZip(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(ea)));
                ev.Response.AddHeader("Content-Encoding", "gzip");
                ev.Response.WriteContent(buffer);
            }
            else
            {
                byte[] _bf = GZipUntil.GetZip(Encoding.UTF8.GetBytes(ea.Json));
                ev.Response.AddHeader("Content-Encoding", "gzip");
                ev.Response.WriteContent(_bf);
            }


        }
        /// <summary>
        /// @heart 用于心跳
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnMessage(object sender, MessageEventArgs e)
        {
            if (e.Data != "@heart")
            {
                var data = JsonConvert.DeserializeObject<dynamic>(e.Data);
                string name = data.clazz;
                string methodname = data.method;
                var parameters = data.param;
                bool keeplive = false;
                DataEventArgs ea = Call(name, methodname, null, parameters, ref keeplive);
                // if (keeplive)
                // {
                //DateTime outTime;
                //if (!RunCall.TryGetValue(name + "." + methodname, out outTime))
                //{
                // Thread th = new Thread(new ParameterizedThreadStart(KeepLiveCall));
                //  th.Start(new CallPara { Name = name, MethodName = methodname, Parameters = parameters });
                //  RunCall.Add(name + "." + methodname, DateTime.Now);
                //}
                //}
                Send(ea.Json);
            }
        }
        private void KeepLiveCall(object obj)
        {
            token = source.Token;

            int error = 0;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    bool keeplive = false;
                    CallPara para = (CallPara)obj;
                    DataEventArgs ea = Call(para.Name, para.MethodName, null, para.Parameters, ref keeplive);
                    if (ea.Json != null && ea.Json != "null")
                    {
                        this.Send(ea.Json);
                    }

                    Thread.Sleep(50);
                    if (error > 20)
                        break;
                }
                catch (Exception e)
                {
                    error++;
                    Log.Error(e.Message);
                }

            }
        }

        protected override void OnClose(CloseEventArgs e)
        {
            source.Cancel();
        }
        protected override void OnPut(HttpRequestEventArgs ev)
        {
            ev.Response.AddHeader("Content-Encoding", "gzip");
            ev.Response.WriteContent(Error(415, "不允许调用"));
        }
        protected override void OnGet(HttpRequestEventArgs ev)
        {
            ev.Response.AddHeader("Content-Encoding", "gzip");
            ev.Response.WriteContent(Error(415, "不允许调用"));
        }
        protected override void OnOptions(HttpRequestEventArgs ev)
        {
            ev.Response.AddHeader("Content-Encoding", "gzip");
            ev.Response.WriteContent(Error(415, "不允许调用"));
        }
        private byte[] Error(int code, string msg)
        {

            var message = new { code = code, msg = msg };
            byte[] _buffer = GZipUntil.GetZip(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message)));

            return _buffer;
        }

        protected DataEventArgs Call(string name, string methodname, string code, dynamic parameters, ref bool keeplive)
        {
            DataEventArgs ea = new DataEventArgs();
            if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(code))
            {
                ea.StatusCode = StatusCode.NoExit;
                ea.LastError = "方法不存在";
                ea.Json = "{\"code\":" + (int)ea.StatusCode + ",\"msg\":\"" + ea.LastError + "\"}";
                return ea;
            }
            if (string.IsNullOrEmpty(methodname) && string.IsNullOrEmpty(code))
            {
                ea.StatusCode = StatusCode.NoExit;
                ea.LastError = "方法不存在";
                ea.Json = "{\"code\":" + (int)ea.StatusCode + ",\"msg\":\"" + ea.LastError + "\"}";
                return ea;
            }
            MethodInfo method = null;
            if (string.IsNullOrEmpty(code))
            {
                #region 地址请求
                try
                {
                    lock (MemberInfos)
                    {
                        foreach (KeyValuePair<string, Tuple<string, MethodInfo, int>> kv in MemberInfos)
                        {
                            object[] clazz = kv.Value.Item2.DeclaringType.GetCustomAttributes(typeof(Restful), true);
                            string clazzpath = string.Empty;
                            if (clazz != null && clazz.Length > 0 && ((Restful)clazz[0]).Path != null)
                            {
                                clazzpath = ((Restful)clazz[0]).Path.ToLower();
                            }

                            string clazzname = kv.Value.Item1.ToString().ToLower();
                            if (clazzname == name || (!string.IsNullOrEmpty(clazzpath) && clazzpath == name))
                            {

                                string kvmethod = kv.Value.Item2.Name.ToLower();
                                string path = string.Empty;
                                object[] cattri = kv.Value.Item2.GetCustomAttributes(typeof(Restful), true);
                                if (null != cattri && cattri.Length > 0)
                                {
                                    Restful rf = (Restful)cattri[0];
                                    if (rf.Path != null)
                                    {
                                        path = rf.Path.ToLower();
                                    }
                                    keeplive = rf.KeepAlive;

                                }

                                if (kvmethod == methodname.ToLower() || (!string.IsNullOrEmpty(path) && path == methodname.ToLower()))
                                {

                                    List<object> param = new List<object>();

                                    try
                                    {
                                        foreach (ParameterInfo para in kv.Value.Item2.GetParameters())
                                        {
                                            param.Add(parameters[para.Name]);
                                        }
                                    }
                                    catch (Exception ex)
                                    {

                                        break;
                                    }
                                    parameters = param;
                                    int i = 0;
                                    foreach (ParameterInfo para in kv.Value.Item2.GetParameters())
                                    {
                                        object o = param[i];
                                        if (para.ParameterType.Name != o.GetType().Name)
                                            break;
                                        i++;
                                    }
                                    if (kv.Value.Item2.GetParameters().Length == param.Count)
                                    {
                                        name = kv.Value.Item1;
                                        method = kv.Value.Item2;
                                        MemberInfos[kv.Key] = new Tuple<string, MethodInfo, int>(MemberInfos[kv.Key].Item1, MemberInfos[kv.Key].Item2, MemberInfos[kv.Key].Item3 + 1);
                                        break;
                                    }

                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                { }
                #endregion
            }

            else
            {
                List<object> _param = new List<object>();
                foreach (object o in parameters)
                {
                    _param.Add(o);
                }
                parameters = _param;
                name = MemberInfos[code].Item1;
                method = MemberInfos[code].Item2;
            }
            if (method == null)
            {
                ea.StatusCode = StatusCode.NoExit;
                ea.LastError = "方法不存在";
                ea.Json = "{\"code\":" + (int)ea.StatusCode + ",\"msg\":\"" + ea.LastError + "\"}";
                return ea;
            }

            parameters = new MethodParam().CorrectParameters(method, parameters);
            object[] arrparam = parameters.ToArray();
            Object bll = ApplicationContext.GetObject(name);
            var result = method.Invoke(bll, arrparam);
            JsonSerializerSettings jsonsetting = new JsonSerializerSettings();
            jsonsetting.Formatting = Formatting.Indented;

            string data = JsonConvert.SerializeObject(result, jsonsetting);
            ea.Param = new System.Collections.ArrayList();
            ea.Json = data;
            for (int i = 0; i < arrparam.Length; i++)
            {
                ea.Param.Add(arrparam[i]);
            }
            return ea;
        }

        internal class CallPara
        {
            public string Name { get; set; }
            public string MethodName { get; set; }

            public dynamic Parameters { get; set; }


        }
    }


}
