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
namespace UcAsp.RPC
{
    public class HttpServer : ServerBase, IDisposable
    {

        private readonly ILog _log = LogManager.GetLogger(typeof(HttpServer));
        CancellationTokenSource token = new CancellationTokenSource();
        private TcpListener _server;
        private bool isstop = false;
        private string _httpversion;
        private string _url = string.Empty;
        private string _mimetype = string.Empty;

        public override Dictionary<string, Tuple<string, MethodInfo, int>> MemberInfos { get; set; }

        public override void StartListen(int port)
        {

            _url = string.Format("http://{0}:{1}", Dns.GetHostName(), port);
            _server = new TcpListener(IPAddress.Any, port);
            _server.Start(3000);

            _log.Info("启动WEB服务" + port);

            Thread run = new Thread(new ParameterizedThreadStart(Listen));
            run.Start(null);
           
        }
        public void Listen(object obj)
        {


            while (!token.IsCancellationRequested)
            {
                try
                {
                    Socket socket = _server.AcceptSocket();
                    Thread start = new Thread(new ParameterizedThreadStart(ThreadAction));
                    start.Start(socket);
                }
                catch (Exception ex)
                {
                    _log.Error(ex);
                }
            }

        }

        private void ThreadAction(object _socket)
        {
            String sDirName;
            String OutMessage = string.Empty;
            Socket socket = (Socket)_socket;
            socket.ReceiveTimeout = 10000;
            socket.SendTimeout = 10000;
            string content = string.Empty;
            _mimetype = "text/html";
            if (socket.Connected)
            {
                Stopwatch Watch = new Stopwatch();
                Watch.Start();
                try
                {
                    StringBuilder reqstr = new StringBuilder();
                    while (true)
                    {
                        Byte[] bReceive = new Byte[buffersize];
                        int i = socket.Receive(bReceive, bReceive.Length, SocketFlags.None);
                        reqstr.Append(Encoding.UTF8.GetString(bReceive).Trim('\0'));
                        if (i - buffersize <= 0)
                        {

                            break;
                        }
                    }
                    Console.WriteLine("线程：" + Thread.CurrentThread.ManagedThreadId);
                    if (string.IsNullOrEmpty(reqstr.ToString()))
                    {
                        socket.Close();
                        return;
                    }
                    Dictionary<string, string> header = Header(reqstr.ToString()).Item1;
                    Dictionary<string, string> request = Header(reqstr.ToString()).Item2;
                    _url = "http://" + header["Host"];

                    // 查找 "HTTP" 的位置

                    _httpversion = request["version"];
                    // 得到请求类型和文件目录文件名
                    sDirName = request["path"];
                    if (!sDirName.EndsWith("/")) sDirName = sDirName + "/";

                    string[] Route = sDirName.Split('/');
                    if (Route.Length < 2 || Route[1].ToUpper() == "HLEP" || Route[1].ToUpper() == "API" || Route[1].ToUpper() == "")
                    {
                        SendAPI(socket);
                        socket.Close();
                        return;
                    }

                    if (sDirName != null)
                    {
                        Regex r = new Regex("\r\n\r\n");
                        string[] Code = r.Split(reqstr.ToString());
                        content = Code[1];
                        if (header.ContainsKey("Content-Length"))
                        {
                            int len = int.Parse(header["Content-Length"]);
                            while (Encoding.UTF8.GetBytes(content).Length < len)
                            {
                                Byte[] bReceive = new Byte[len];

                                int i = socket.Receive(bReceive, bReceive.Length, 0);
                                content = content + Encoding.UTF8.GetString(bReceive).Trim('\0');
                                LastParam = content;
                                if (Encoding.UTF8.GetBytes(content).Length - len == 0)
                                { break; }

                            }

                        }
                        if (header.ContainsKey("Authorization"))
                        {
                            string[] auth = header["Authorization"].Split(' ');
                            if (auth.Length == 2)
                            {
                                if (auth[1] != Authorization)
                                {
                                    HttpRespone.SendAuthError(_httpversion, "认证失败", ref socket);
                                    socket.Close();
                                    return;
                                }
                            }
                            else
                            {
                                HttpRespone.SendAuthError(_httpversion, "认证失败", ref socket);
                                socket.Close();
                                return;
                            }
                        }
                        else
                        {
                            HttpRespone.SendAuthError(_httpversion, "认证失败", ref socket);
                            socket.Close();
                            return;
                        }
                        string param = content;
                        if (Route[1].ToUpper() == "WEBAPI")
                        {
                            string method = string.Empty;
                            for (int n = 2; n < Route.Length - 1; n++)
                            {
                                if (string.IsNullOrEmpty(method))
                                {
                                    method = Route[n];
                                }
                                else
                                {
                                    method = method + "/" + Route[n];
                                }
                            }
                            param = content + "\\" + (method + ",webapi");

                        }
                        else
                        {
                            param = content + "\\" + (sDirName.Replace("/", ""));
                        }
                        if (header.ContainsKey("UcAsp.Net_RPC"))
                        {
                            param = param + "\\true";
                        }
                        else
                        {
                            param = param + "\\false";
                        }

                        Call(socket, param);
                    }

                }
                catch (Exception ex)
                {
                    _log.Error(ex);
                    HttpRespone.SendError(_httpversion, ex.Message, ref socket);
                    Console.WriteLine(ex);
                }
                finally
                {
                    Watch.Stop();


                }
            }
        }

        public override void Call(Socket socket, object obj)
        {
            string[] content = obj.ToString().Split('\\');
            string rpc = content[2];
            Stopwatch Watch = new Stopwatch();
            Watch.Start();
            try
            {
                var e = JsonConvert.DeserializeObject<dynamic>(content[0]);
                MethodInfo method = null;
                string name = string.Empty;
                string code = content[1];
                if (code.ToLower() == "register")
                {
                    string reginfo = JsonConvert.SerializeObject(RegisterInfo);

                    DataEventArgs reg = new DataEventArgs();
                    reg.Param = new System.Collections.ArrayList();
                    reg.Json = reginfo;
                    byte[] _buffer = HttpRespone.GetZip(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(reg)));
                    HttpRespone.SendHeader(_httpversion, _mimetype, _buffer.Length, " 200 OK", ref socket);
                    HttpRespone.SendToBrowser(_buffer, ref socket);
                    socket.Close();
                    return;
                }
                var parameters = e;
                if (code.IndexOf(",webapi") > -1)
                {

                    string[] n = code.Replace(",webapi", "").Split('/');
                    name = n[0].ToLower();
                    string methodname = n[1];

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
                                foreach (ParameterInfo para in kv.Value.Item2.GetParameters())
                                {
                                    param.Add(e[para.Name]);
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

                if (string.IsNullOrEmpty(name))
                {
                    string message = "空间名不存在";
                    byte[] _buffer = HttpRespone.GetZip(Encoding.UTF8.GetBytes(message));
                    HttpRespone.SendHeader(_httpversion, _mimetype, _buffer.Length, " 500 OK", ref socket);
                    HttpRespone.SendToBrowser(_buffer, ref socket);
                    socket.Close();
                    return;
                }

                if (method == null)
                {
                    string message = "方法不存在";
                    byte[] _buffer = HttpRespone.GetZip(Encoding.UTF8.GetBytes(message));
                    HttpRespone.SendHeader(_httpversion, _mimetype, _buffer.Length, " 500 OK", ref socket);
                    HttpRespone.SendToBrowser(_buffer, ref socket);
                    socket.Close();
                    return;
                }


                if (parameters == null) parameters = new List<object>();




                parameters = this.CorrectParameters(method, parameters);
                object[] arrparam = parameters.ToArray();
                Object bll = ApplicationContext.GetObject(name);

                var result = method.Invoke(bll, arrparam);

                string data = JsonConvert.SerializeObject(result);
                DataEventArgs ea = new DataEventArgs();
                ea.Param = new System.Collections.ArrayList();
                ea.Json = data;
                for (int i = 0; i < arrparam.Length; i++)
                {
                    ea.Param.Add(arrparam[i]);
                }

                byte[] buffer = HttpRespone.GetZip(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(ea)));
                if (rpc == "true")
                {
                    HttpRespone.SendHeader(_httpversion, _mimetype, buffer.Length, " 200 OK", ref socket);
                    HttpRespone.SendToBrowser(buffer, ref socket);
                    socket.Close();
                }
                else
                {
                    byte[] _bf = HttpRespone.GetZip(Encoding.UTF8.GetBytes(data));
                    HttpRespone.SendHeader(_httpversion, _mimetype, _bf.Length, " 200 OK", ref socket);
                    HttpRespone.SendToBrowser(_bf, ref socket);
                    socket.Close();
                }

            }
            catch (Exception ex)
            {
                string message = ex.InnerException != null ? ex.InnerException.Message : "" + ex.Message + ex.Source;
                byte[] _bf = HttpRespone.GetZip(Encoding.UTF8.GetBytes(message));
                HttpRespone.SendHeader(_httpversion, _mimetype, _bf.Length, " 500 OK", ref socket);
                HttpRespone.SendToBrowser(_bf, ref socket);
                socket.Close();
            }
            finally
            {

                Watch.Stop();
                // Console.WriteLine("Call：" + Watch.ElapsedMilliseconds);
            }
        }
        private void SendAPI(Socket socket)
        {
            byte[] buffer = HttpRespone.GetZip(Encoding.UTF8.GetBytes(HtmlHelp()));
            HttpRespone.SendHeader(_httpversion, _mimetype, buffer.Length, " 200 OK", ref socket);
            HttpRespone.SendToBrowser(buffer, ref socket);
            socket.Close();
        }
        private string HtmlHelp()
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

        protected internal class HttpRespone
        {
            private readonly static ILog _log = LogManager.GetLogger(typeof(HttpRespone));
            public static void SendHeader(string sHttpVersion, string sMIMEHeader, int iTotBytes, string sStatusCode, ref Socket mySocket)
            {

                String sBuffer = "";
                if (sMIMEHeader.Length == 0)
                {
                    sMIMEHeader = "text/html"; // 默认 text/html
                }
                sBuffer = sBuffer + sHttpVersion + sStatusCode + "\r\n";
                sBuffer = sBuffer + "Author: Rixiang Yu \r\n";
                sBuffer = sBuffer + "Server: UcAsp.Net \r\n";
                sBuffer = sBuffer + "Content-Type: " + sMIMEHeader + "\r\n";
                sBuffer = sBuffer + "Accept-Ranges: bytes\r\n";
                sBuffer = sBuffer + "Content-Encoding: gzip\r\n";
                sBuffer = sBuffer + "Content-Length: " + iTotBytes + "\r\n\r\n";

                byte[] _buffer = Encoding.UTF7.GetBytes(sBuffer);
                SendToBrowser(_buffer, ref mySocket);

            }
            //public static void SendToBrowser(string data, ref Socket socket)
            //{
            //    try
            //    {
            //        if (socket.Connected)
            //        {
            //            byte[] buffer = Encoding.UTF8.GetBytes(data);

            //            socket.Send(buffer, buffer.Length, 0);
            //        }

            //    }
            //    catch (Exception e)
            //    {
            //        _log.Error(e);
            //    }
            //}
            public static void SendToBrowser(byte[] data, ref Socket socket)
            {
                try
                {
                    if (socket.Connected)
                    {
                        byte[] buffer = data;
                        socket.Send(buffer, 0, buffer.Length, SocketFlags.None);
                    }
                }
                catch (Exception e)
                {
                    _log.Error(e);
                }
            }
            public static void SendError(string sHttpVersion, ref Socket mySocket)
            {
                string OutMessage = "<H2>Error!! 404 Not Found</H2><Br>";
                byte[] buffer = GetZip(Encoding.UTF8.GetBytes(OutMessage));
                SendHeader(sHttpVersion, "", buffer.Length, " 404 Not Found", ref mySocket);
                SendToBrowser(buffer, ref mySocket);
            }
            public static void SendError(string sHttpVersion, string errorMsg, ref Socket mySocket)
            {
                string OutMessage = "<H2>Error!! 404 Not Found</H2><Br>" + errorMsg;
                byte[] buffer = GetZip(Encoding.UTF8.GetBytes(OutMessage));
                SendHeader(sHttpVersion, "", buffer.Length, " 404 Not Found", ref mySocket);
                SendToBrowser(buffer, ref mySocket);
            }
            public static void SendAuthError(string sHttpVersion, string errorMsg, ref Socket mySocket)
            {
                string OutMessage = errorMsg;
                byte[] buffer = GetZip(Encoding.UTF8.GetBytes(OutMessage));
                SendHeader(sHttpVersion, "", buffer.Length, " 401 Unauthorized", ref mySocket);
                SendToBrowser(buffer, ref mySocket);
            }
            public static byte[] GetZip(byte[] buffer)
            {
                byte[] gizpbytes = null;
                using (MemoryStream cms = new MemoryStream())
                {
                    using (System.IO.Compression.GZipStream gzip = new System.IO.Compression.GZipStream(cms, System.IO.Compression.CompressionMode.Compress))
                    {
                        //将数据写入基础流，同时会被压缩
                        gzip.Write(buffer, 0, buffer.Length);
                    }
                    gizpbytes = cms.ToArray();
                }
                return gizpbytes;
            }
        }

        private Tuple<Dictionary<string, string>, Dictionary<string, string>> Header(string header)
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();

            string[] h = Regex.Split(header, "\r\n");
            Dictionary<string, string> r = Request(h[0]);
            for (int i = 1; i < h.Length; i++)
            {
                string[] d = Regex.Split(h[i], ": ");
                if (d.Length == 2)
                {
                    dic.Add(d[0], d[1]);
                }
            }
            return new Tuple<Dictionary<string, string>, Dictionary<string, string>>(dic, r);
        }

        private Dictionary<string, string> Request(string request)
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            string[] r = Regex.Split(request, " ");
            dic.Add("method", r[0]);
            dic.Add("path", r[1]);
            dic.Add("version", r[2]);
            return dic;
        }
        public override void Stop()
        {
            try
            {
                base.Stop();
                _server.Server.Close();
                _server.Stop();
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
                    _server.Stop();

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
}
