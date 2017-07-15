/***************************************************
*创建人:TecD02
*创建时间:2016/8/17 15:54:19
*功能说明:<Function>
*版权所有:<Copyright>
*Frameworkversion:4.0
*CLR版本：4.0.30319.42000
***************************************************/
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using System.Reflection;
using Newtonsoft.Json;
using log4net;
using UcAsp.WebSocket;
using UcAsp.WebSocket.Net;
using UcAsp.WebSocket.Server;
namespace UcAsp.RPC
{
    public class HttpServer : ServerBase, IDisposable
    {

        private readonly ILog _log = LogManager.GetLogger(typeof(HttpServer));
        CancellationTokenSource token = new CancellationTokenSource();
        private WebServer web;
        private string _httpversion;
        private string _url = string.Empty;
        private string _mimetype = string.Empty;

        public override Dictionary<string, Tuple<string, MethodInfo, int>> MemberInfos { get; set; }

        public override void StartListen(int port)
        {
            web = new WebServer("http://localhost:" + port + "/");
            web.DocumentRootPath = System.AppDomain.CurrentDomain.BaseDirectory;
            IPAddress[] iplist = Dns.GetHostAddresses(Dns.GetHostName());
            for (int i = 0; i < iplist.Length; i++)
            {
                web.AddPrefixes("http://" + iplist[i] + ":" + port + "/");
            }
            web.AddWebSocketService<ApiServer>("/", () => new ApiServer() { MemberInfos = MemberInfos }, null);
            web.AddWebSocketService<ApiServer>("/help", () => new ApiServer() { MemberInfos = MemberInfos }, null);
            web.AddWebSocketService<RegisterServer>("/register", () => new RegisterServer() { RegisterInfo = RegisterInfo }, null);
            web.AddWebSocketService<ActionServer>("/{md5}", () => new ActionServer() { MemberInfos = MemberInfos }, new { md5 = "([a-zA-Z0-9]){32,32}" });

            web.AddWebSocketService<ActionServer>("/webapi/{clazz}/{method}", () => new ActionServer() { MemberInfos = MemberInfos }, new { clazz = "[a-zA-Z0-9.]*", method = "[a-zA-Z0-9]*" });
            web.AddWebSocketService<ActionServer>("/websocket/call", () => new ActionServer() { MemberInfos = MemberInfos }, null);

            web.Start();



        }

        public override void Stop()
        {
            try
            {
                base.Stop();
                web.Stop();
                token.Cancel();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

        }

        #region IDisposable Support
        private bool disposedValue = false; // 要检测冗余调用

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    web.Stop();

                }

                // TODO: 释放未托管的资源(未托管的对象)并在以下内容中替代终结器。
                // TODO: 将大型字段设置为 null。

                disposedValue = true;
            }
        }

        // TODO: 仅当以上 Dispose(bool disposing) 拥有用于释放未托管资源的代码时才替代终结器。
        // ~HttpServer() {
        //   // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
        //   Dispose(false);
        // }

        // 添加此代码以正确实现可处置模式。
        void IDisposable.Dispose()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(true);
            // TODO: 如果在以上内容中替代了终结器，则取消注释以下行。
            // GC.SuppressFinalize(this);
        }
        #endregion

    }

    public class ApiServer : WebSocketBehavior
    {
        public Dictionary<string, Tuple<string, MethodInfo, int>> MemberInfos { get; set; }

        private string HtmlHelp(string _url)
        {

            StringBuilder sb = new StringBuilder();
            sb.Append("<!DOCTYPE html>");
            sb.Append(@"<html lang = ""en"">");
            sb.Append(@"<meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"" />");
            sb.Append(@"<head>");
            sb.Append(@"<link href=""//echarts.baidu.com/echarts2/doc/asset/css/bootstrap.css"" rel=""stylesheet"">");
            sb.Append(@" </head><body>");
            sb.Append(@"<div class=""navbar navbar-default"" role=""navigation"" id=""head""> <div class=""container""><div class=""navbar - header"">UCAsp.NET</div></div></div>");
            sb.Append(@"<div class=""container-fluid"">");
            foreach (KeyValuePair<string, Tuple<string, MethodInfo, int>> kv in MemberInfos)
            {

                sb.Append(@"<div class=""list-group"">");
                sb.AppendFormat(@"<div class=""list-group-item"">{0} </div> <a href=""javascript:void(0)"" class=""list-group-item active"">方法名：{1}   ", kv.Value.Item1, kv.Value.Item2.Name.Replace("<", "&lt;").Replace(">", " &gt;"));
                ParameterInfo[] para = kv.Value.Item2.GetParameters();
                sb.Append(@"(");
                for (int x = 0; x < para.Length; x++)
                {
                    sb.AppendFormat("{0} {1}", Proxy.GetTypeName(para[x].ParameterType).Replace("<", "&lt;").Replace(">", " &gt;"), para[x].Name.Replace("<", "&lt;").Replace(">", " &gt;"));
                    if (x != para.Length - 1)
                    {
                        sb.Append(",");
                    }
                }
                sb.AppendFormat("     )  Method:POST   返回类型：{0}[自启动运行{1}次]</a>", Proxy.GetTypeName(kv.Value.Item2.ReturnType).Replace("<", "&lt;").Replace(">", " &gt;"), kv.Value.Item3);
                sb.Append(@"<div class=""list-group-item"">Example:Request Body JSON [");
                for (int xx = 0; xx < para.Length; xx++)
                {
                    sb.AppendFormat("{0}", Proxy.GetTypeName(para[xx].ParameterType).Replace("<", "&lt;").Replace(">", " &gt;"));
                    if (xx != para.Length - 1)
                    {
                        sb.Append(",");
                    }
                }
                sb.Append(@"]</div>");
                sb.AppendFormat(@"<div class=""list-group-item"">API URL<br />[1]:{0}/{1}<br />[2]:{0}/WEBAPI/{2}/{3} </div> ", _url, kv.Key, kv.Value.Item1, kv.Value.Item2.Name);

                object[] clazz = kv.Value.Item2.DeclaringType.GetCustomAttributes(typeof(Restful), true);
                string clazzpath = string.Empty;
                string path = kv.Value.Item2.Name;




                object[] cattri = kv.Value.Item2.GetCustomAttributes(typeof(Restful), true);


                if (clazz != null && clazz.Length > 0)
                {
                    if (null != cattri && cattri.Length > 0)
                    {
                        Restful rf = (Restful)cattri[0];
                        path = rf.Path.ToLower();
                    }
                    clazzpath = ((Restful)clazz[0]).Path.ToLower();
                    sb.Append(@"<div class=""list-group-item"">Example:Request Body JSON {");
                    for (int xx = 0; xx < para.Length; xx++)
                    {
                        sb.AppendFormat("\"{0}\":value", para[xx].Name);
                        if (xx != para.Length - 1)
                        {
                            sb.Append(",");
                        }
                    }
                    sb.Append(@"}</div>");
                    sb.AppendFormat(@"<div class=""list-group-item"">API URL<br />[1]:{0}/WEBAPI/{1}/{2}/<br /> </div> ", _url, clazzpath, path);

                }
                sb.Append(@" </div>");

            }
            sb.Append("</div>");
            sb.Append(@"<script src ="" //cdn.bootcss.com/jquery/2.1.4/jquery.min.js""></script>");
            sb.Append(@"<script src=""//echarts.baidu.com/echarts2/doc/asset/js/bootstrap.min.js""></script>");
            sb.Append("</body></html>");
            return sb.ToString();
        }

        protected override void OnMessage(object sender, MessageEventArgs e)
        {
            Send(HtmlHelp(""));
        }
        protected override void OnGet(HttpRequestEventArgs ev)
        {
            byte[] _buffer = GZipUntil.GetZip(Encoding.UTF8.GetBytes(HtmlHelp("http://" + ev.Request.UserHostName)));
            ev.Response.AddHeader("Content-Encoding", "gzip");
            ev.Response.WriteContent(_buffer);
        }
    }
    public class RegisterServer : WebSocketBehavior
    {
        public List<RegisterInfo> RegisterInfo { get; set; }
        protected override void OnGet(HttpRequestEventArgs ev)
        {
            Send(ev);
        }
        protected override void OnPost(HttpRequestEventArgs ev)
        {
            Send(ev);
        }

        private void Send(HttpRequestEventArgs ev)
        {
            string reginfo = JsonConvert.SerializeObject(RegisterInfo);

            DataEventArgs reg = new DataEventArgs();
            reg.Param = new System.Collections.ArrayList();
            reg.Json = reginfo;
            byte[] _buffer = GZipUntil.GetZip(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(reg)));
            ev.Response.AddHeader("Content-Encoding", "gzip");
            ev.Response.WriteContent(_buffer);
        }
    }
    public class ActionServer : WebSocketBehavior
    {
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

            DataEventArgs ea = Call(name, methodname, code, parameters);

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
        protected override void OnMessage(object sender, MessageEventArgs e)
        {
            var data = JsonConvert.DeserializeObject<dynamic>(e.Data);
            string name = data.clazz;
            string methodname = data.method;
            var parameters = data.param;
            DataEventArgs ea = Call(name, methodname, null, parameters);

            Send(ea.Json);
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

        protected DataEventArgs Call(string name, string methodname, string code, dynamic parameters)
        {
            DataEventArgs ea = new DataEventArgs();
            if (string.IsNullOrEmpty(name)&&string.IsNullOrEmpty(code))
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
                foreach (KeyValuePair<string, Tuple<string, MethodInfo, int>> kv in MemberInfos)
                {
                    object[] clazz = kv.Value.Item2.DeclaringType.GetCustomAttributes(typeof(Restful), true);
                    string clazzpath = string.Empty;
                    if (clazz != null && clazz.Length > 0)
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
                            path = rf.Path.ToLower();
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
            string data = JsonConvert.SerializeObject(result);
            ea.Param = new System.Collections.ArrayList();
            ea.Json = data;
            for (int i = 0; i < arrparam.Length; i++)
            {
                ea.Param.Add(arrparam[i]);
            }
            return ea;
        }

    }
}
