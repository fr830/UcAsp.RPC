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
using System.Threading;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using System.Text;
using System.Reflection;
using Newtonsoft.Json;
using log4net;
namespace UcAsp.RPC
{
    public class HttpServer : ServerBase, IDisposable
    {

        private readonly ILog _log = LogManager.GetLogger(typeof(HttpServer));
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
            _server.Start();
            _log.Info("启动WEB服务" + port);
            for (int i = 0; i < Environment.ProcessorCount * 2; i++)
            {
                ThreadPool.QueueUserWorkItem(Listen, null);
            }
        }
        public void Listen(object obj)
        {
            int iStartPos = 0;
            String sRequest;
            String sDirName;
            String OutMessage = string.Empty;
            while (true)
            {
                if (isstop)
                    break;
                Socket socket = _server.AcceptSocket();
                socket.ReceiveTimeout = 10000;
                socket.SendTimeout = 10000;
                string content = string.Empty;
                _mimetype = "text/html";
                if (socket.Connected)
                {
                    try
                    {
                        string sBuffer = string.Empty;
                        while (true)
                        {
                            Byte[] bReceive = new Byte[buffersize];
                            int i = socket.Receive(bReceive, bReceive.Length, 0);
                            sBuffer = sBuffer + Encoding.ASCII.GetString(bReceive).Substring(0, i);
                            if (i - buffersize < 0)
                            {
                                break;
                            }
                        }
                        if (string.IsNullOrEmpty(sBuffer))
                        {

                            return;
                        }
                        Dictionary<string, string> header = Header(sBuffer).Item1;
                        Dictionary<string, string> request = Header(sBuffer).Item2;
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
                            continue;
                        }
                        if (sDirName != null)
                        {
                            Regex r = new Regex("\r\n\r\n");
                            string[] Code = r.Split(sBuffer);
                            content = Code[1];
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
                            if (header.ContainsKey("Content-Length"))
                            {

                                int len = int.Parse(header["Content-Length"]);
                                while (content.Length < len)
                                {
                                    Byte[] bReceive = new Byte[len];
                                    int i = socket.Receive(bReceive, bReceive.Length, 0);
                                    sBuffer = sBuffer + Encoding.ASCII.GetString(bReceive).Substring(0, i);
                                    LastParam = content = sBuffer;
                                }

                            }
                            List<object> param = JsonConvert.DeserializeObject<List<object>>(content);
                            if (param == null)
                            {
                                param = new List<object>();
                            }
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
                                param.Add(method + ",webapi");

                            }
                            else
                            {
                                param.Add(sDirName.Replace("/", ""));
                            }
                            Call(socket, param);
                        }

                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex);
                        HttpRespone.SendError(_httpversion, ex.Message, ref socket);

                    }
                    finally
                    {
                        socket.Close();
                    }
                }
            }

        }

        public override void Call(Socket socket, object obj)
        {
            try
            {
                List<object> e = (List<object>)obj;
                MethodInfo method = null;
                string name = string.Empty;
                string code = e[e.Count - 1].ToString();
                var parameters = e;
                if (code.IndexOf(",webapi") > -1)
                {
                    string[] n = code.Replace(",webapi", "").Split('/');
                    name = n[0];
                    string methodname = n[1];

                    foreach (KeyValuePair<string, Tuple<string, MethodInfo, int>> kv in MemberInfos)
                    {
                        if (kv.Value.Item2.Name.ToLower() == methodname.ToLower())
                        {
                            int i = 0;
                            foreach (ParameterInfo para in kv.Value.Item2.GetParameters())
                            {
                                object o = e[i];
                                if (para.ParameterType.Name != o.GetType().Name)
                                    break;
                                i++;
                            }
                            if (name.ToString() == kv.Value.Item1.ToString() && (kv.Value.Item2.GetParameters().Length == e.Count - 1))
                            {
                                name = kv.Value.Item1;
                                method = kv.Value.Item2;
                                MemberInfos[kv.Key] = new Tuple<string, MethodInfo, int>(MemberInfos[kv.Key].Item1, MemberInfos[kv.Key].Item2, MemberInfos[kv.Key].Item3 + 1);
                                break;
                            }
                        }
                    }


                }
                else
                {
                    name = MemberInfos[code].Item1;
                    method = MemberInfos[code].Item2;
                }
                if (string.IsNullOrEmpty(name))
                {
                    string message = "空间名不存在";
                    HttpRespone.SendHeader(_httpversion, _mimetype, Encoding.UTF8.GetByteCount(message), " 500 OK", ref socket);
                    HttpRespone.SendToBrowser(message, ref socket);
                }
                if (method == null)
                {
                    string message = "方法不存在";
                    HttpRespone.SendHeader(_httpversion, _mimetype, Encoding.UTF8.GetByteCount(message), " 500 OK", ref socket);
                    HttpRespone.SendToBrowser(message, ref socket);
                }
                parameters.RemoveAt(e.Count - 1);
                if (parameters == null) parameters = new List<object>();
                parameters = this.CorrectParameters(method, parameters);

                Object bll = ApplicationContext.GetObject(name);

                var result = method.Invoke(bll, parameters.ToArray());
                string data = JsonConvert.SerializeObject(result);
                byte[] buffer = Encoding.UTF8.GetBytes(data);
                HttpRespone.SendHeader(_httpversion, _mimetype, buffer.Length, " 200 OK", ref socket);
                HttpRespone.SendToBrowser(buffer, ref socket);

            }
            catch (Exception ex)
            {
                string message = ex.InnerException != null ? ex.InnerException.Message : "" + ex.Message + ex.Source;
                HttpRespone.SendHeader(_httpversion, _mimetype, Encoding.UTF8.GetByteCount(message), " 500 OK", ref socket);
                HttpRespone.SendToBrowser(message, ref socket);
            }
        }
        public override List<object> CorrectParameters(MethodInfo method, List<object> parameterValues)
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
                        // Assembly al = pType.Assembly;
                        // object obj = al.CreateInstance(pType.Namespace + "." + pType.Name);
                        object pValue = JsonConvert.DeserializeObject(entity.ToString(), pType); //this._serializer.ToEntity(bin, pType);
                        // 保存参数
                        parameterValues[i] = pValue;
                    }
                }
            }

            return parameterValues;
        }
        private void SendAPI(Socket socket)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(HtmlHelp());
            HttpRespone.SendHeader(_httpversion, _mimetype, buffer.Length, " 200 OK", ref socket);
            HttpRespone.SendToBrowser(buffer, ref socket);
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
                sBuffer = sBuffer + "Accept-Encoding: gzip,deflate\r\n";
                sBuffer = sBuffer + "Content-Length: " + iTotBytes + "\r\n\r\n";

                SendToBrowser(sBuffer, ref mySocket);

            }
            public static void SendToBrowser(string data, ref Socket socket)
            {
                try
                {
                    if (socket.Connected)
                    {
                        byte[] buffer = Encoding.UTF8.GetBytes(data);

                        socket.Send(buffer, buffer.Length, 0);
                    }

                }
                catch (Exception e)
                {
                    _log.Error(e);
                }
            }
            public static void SendToBrowser(byte[] data, ref Socket socket)
            {
                try
                {
                    if (socket.Connected)
                    {
                        //byte[] gizpbytes = null;
                        //using (MemoryStream cms = new MemoryStream())
                        //{
                        //    using (System.IO.Compression.GZipStream gzip = new System.IO.Compression.GZipStream(cms, System.IO.Compression.CompressionMode.Compress))
                        //    {
                        //        //将数据写入基础流，同时会被压缩
                        //        gzip.Write(data, 0, data.Length);
                        //    }
                        //    gizpbytes = cms.ToArray();
                        //}

                        socket.Send(data, 0, data.Length, SocketFlags.None);
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
                SendHeader(sHttpVersion, "", Encoding.UTF8.GetBytes(OutMessage).Length, " 404 Not Found", ref mySocket);
                SendToBrowser(OutMessage, ref mySocket);
            }
            public static void SendError(string sHttpVersion, string errorMsg, ref Socket mySocket)
            {
                string OutMessage = "<H2>Error!! 404 Not Found</H2><Br>" + errorMsg;
                SendHeader(sHttpVersion, "", Encoding.UTF8.GetBytes(OutMessage).Length, " 404 Not Found", ref mySocket);
                SendToBrowser(OutMessage, ref mySocket);
            }
            public static void SendAuthError(string sHttpVersion, string errorMsg, ref Socket mySocket)
            {
                string OutMessage = errorMsg;
                SendHeader(sHttpVersion, "",Encoding.UTF8.GetBytes(OutMessage).Length, " 401 Unauthorized", ref mySocket);
                SendToBrowser(OutMessage, ref mySocket);
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
            base.Stop();
            isstop = true;
            _server.Stop();


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
