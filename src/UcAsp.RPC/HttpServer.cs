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
    public class HttpServer : ServerBase
    {
        private readonly ILog _log = LogManager.GetLogger(typeof(HttpServer));
        private TcpListener _server;
        private string _httpversion;
        private string _url = string.Empty;
        private string _mimetype = string.Empty;
        public override Dictionary<string, Tuple<string, MethodInfo>> MemberInfos { get; set; }

        public override void StartListen(int port)
        {
            _url = string.Format("http://{0}:{1}", Dns.GetHostName(), port);
            _server = new TcpListener(IPAddress.Any, port);
            _server.Start();
            _log.Info("启动WEB服务" + port);
            for (int i = 0; i < 10; i++)
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
                Socket socket = _server.AcceptSocket();
                socket.ReceiveTimeout = 10000;
                socket.SendTimeout = 10000;
                string content = string.Empty;
                _mimetype = "text/html";
                if (socket.Connected)
                {
                    try
                    {
                        Byte[] bReceive = new Byte[buffersize];
                        int i = socket.Receive(bReceive, bReceive.Length, 0);
                        //转换成字符串类型
                        string sBuffer = Encoding.ASCII.GetString(bReceive).Substring(0, i);
                        // 查找 "HTTP" 的位置
                        iStartPos = sBuffer.IndexOf("HTTP", 1);
                        _httpversion = sBuffer.Substring(iStartPos, 8);
                        // 得到请求类型和文件目录文件名
                        sRequest = sBuffer.Substring(0, iStartPos - 1);
                        if (!sRequest.EndsWith("/")) sRequest = sRequest + "/";
                        //得到请求文件目录
                        if (sBuffer.Substring(0, 4) != "POST")
                        {
                            sDirName = sRequest.Substring(sRequest.IndexOf("/"), sRequest.LastIndexOf("/") - 3);
                        }
                        else
                        {
                            sDirName = sRequest.Substring(sRequest.IndexOf("/"), sRequest.LastIndexOf("/") - 4);
                        }
                        string[] Route = sDirName.Split('/');
                        if (Route.Length < 2)
                        {
                            SendAPI(socket);
                            socket.Close();
                            continue;
                        }
                        if (Route[1].ToUpper() == "HLEP" || Route[1].ToUpper() == "API" || Route[1].ToUpper() == "")
                        {
                            SendAPI(socket);
                        }
                        if (sDirName != null)
                        {
                            Regex r = new Regex("\r\n\r\n");
                            string[] Code = r.Split(sBuffer);
                            content = Code[1];
                            List<object> param = JsonConvert.DeserializeObject<List<object>>(content);
                            if (param == null)
                            {
                                param = new List<object>();
                            }
                            param.Add(sDirName.Replace("/", ""));
                            Call(socket, param);
                        }

                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex);
                        HttpRespone.SendError(_httpversion, ref socket);
                        Thread thread = Thread.CurrentThread;
                        thread.Abort();
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

                string code = e[e.Count - 1].ToString();

                string name = MemberInfos[code].Item1;

                MethodInfo method = MemberInfos[code].Item2;
                var parameters = e;
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
                string message = string.Format("{ \"Error\":\"{0};{1}\"}", ex.Message, ex.Source);
                HttpRespone.SendHeader(_httpversion, _mimetype, message.Length, " 500 OK", ref socket);
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
            sb.Append(@"<link href=""//cdn.bootcss.com/bootstrap/4.0.0-alpha.3/css/bootstrap.css"" rel=""stylesheet"">");
            sb.Append(@" </head><body>");
            sb.Append(@"<div class=""container-fluid"">");
            foreach (KeyValuePair<string, Tuple<string, MethodInfo>> kv in MemberInfos)
            {

                sb.Append(@"<div class=""row"">");
                sb.AppendFormat(@"<div class=""col - md - 4"">{0} </div> <div class=""col - md - 4"">返回类型：{1}  方法名：{2}  ", kv.Value.Item1, Proxy.GetTypeName(kv.Value.Item2.ReturnType).Replace("<", "&lt;").Replace(">", " &gt;"), kv.Value.Item2.Name.Replace("<", "&lt;").Replace(">", " &gt;"));
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
                sb.Append("     )  Method:POST</div>");
                sb.Append(@"<div class=""col - md - 12"">Example:Request Body JSON [");
                for (int xx = 0; xx < para.Length; xx++)
                {
                    sb.AppendFormat("{0}", Proxy.GetTypeName(para[xx].ParameterType).Replace("<", "&lt;").Replace(">", " &gt;"));
                    if (xx != para.Length - 1)
                    {
                        sb.Append(",");
                    }
                }
                sb.Append(@"]</div>");
                sb.AppendFormat(@"<div class=""col - md - 12"">API URL：{0}/{1}  </div> ", _url, kv.Key);
                sb.Append(@" </div>");
            }
            sb.Append("</div>");
            sb.Append(@"<script src ="" //cdn.bootcss.com/jquery/2.1.4/jquery.min.js""></script>");
            sb.Append(@"<script src=""//cdn.bootcss.com/bootstrap/4.0.0-alpha.3/js/bootstrap.js""></script>");
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
                sBuffer = sBuffer + "Server: ISCS\r\n";
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
                SendHeader(sHttpVersion, "", OutMessage.Length, " 404 Not Found", ref mySocket);
                SendToBrowser(OutMessage, ref mySocket);
            }
        }
    }
}
