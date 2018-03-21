/***************************************************
*创建人:rixiang.yu
*创建时间:2017/7/15 18:23:08
*功能说明:<Function>
*版权所有:<Copyright>
*Frameworkversion:4.0
*CLR版本：4.0.30319.42000
***************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UcAsp.WebSocket.Server;
using UcAsp.WebSocket.Net;
using UcAsp.WebSocket;
using System.Xml;
using System.IO;
namespace UcAsp.RPC.Service
{
    public class ApiService : WebSocketBehavior
    {

        public  Dictionary<string, Tuple<string, MethodInfo, int, long>> MemberInfos { get; set; }

        protected override void OnConnect(HttpRequestEventArgs ev)
        {

        }
        private string HtmlHelp(string _url)
        {

            try
            {
                int number = 0;
                Dictionary<string, Tuple<string, MethodInfo, int, long>> dic = new Dictionary<string, Tuple<string, MethodInfo, int, long>>(MemberInfos);
                StringBuilder sb = new StringBuilder();
                sb.Append("<!DOCTYPE html>");
                sb.Append(@"<html lang = ""en"">");
                sb.Append(@"<meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"" />");
                sb.Append(@"<head>");
                sb.Append(@"<link href=""//echarts.baidu.com/echarts2/doc/asset/css/bootstrap.css"" rel=""stylesheet"">");
                sb.Append(@" </head><body>");
                sb.Append(@"<div class=""navbar navbar-default"" role=""navigation"" id=""head""> <div class=""container""><div class=""navbar - header"">UcAsp.NET</div></div></div>");

                sb.Append(@"<div class=""panel-group"" id=""accordion"">");
                lock (dic)
                {
                    foreach (KeyValuePair<string, Tuple<string, MethodInfo, int, long>> kv in dic.OrderBy(o=>o.Value.Item1))
                    {
                        object[] clazz = kv.Value.Item2.DeclaringType.GetCustomAttributes(typeof(Restful), true);
                        string clazzpath = string.Empty;
                        string path = kv.Value.Item2.Name;




                        object[] cattri = kv.Value.Item2.GetCustomAttributes(typeof(Restful), true);
                        if (clazz != null && clazz.Length > 0)
                        {
                            if (null != cattri && cattri.Length > 0)
                            {
                                UcAsp.RPC.Restful rf = (UcAsp.RPC.Restful)cattri[0];

                                if (rf.NoRest)
                                    continue;
                                if (rf.Path != null)
                                {
                                    path = rf.Path.ToLower();
                                }

                            }
                            string apixml = kv.Value.Item2.Module.FullyQualifiedName.Replace(".dll", "") + ".xml";
                            string filename = kv.Value.Item2.DeclaringType.FullName + "." + kv.Value.Item2.Name;
                            int len = kv.Value.Item2.GetParameters().Length;
                            if (len > 0)
                            {
                                filename += "(";
                                for (int i = 0; i < len; i++)
                                {
                                    ParameterInfo _param = kv.Value.Item2.GetParameters()[i];
                                    if (i != len - 1)
                                    {
                                        filename += _param.ParameterType.FullName + ",";
                                    }
                                    else
                                    {
                                        filename += _param.ParameterType.FullName;
                                    }
                                }

                                filename += ")";
                            }
                            string helptxt = string.Empty;
                            if (File.Exists(apixml))
                            {
                                XmlDocument doc = new XmlDocument();
                                doc.Load(apixml);
                                XmlNode xn = doc.SelectSingleNode("/doc/members/member[@name=\"M:" + filename + "\"]");
                                if (xn != null)
                                {
                                    helptxt = xn.InnerText;
                                }

                            }
                            number++;
                            sb.Append(@"<div class=""panel panel-default""><div class=""panel-heading"">");
                            sb.AppendFormat(@"<!--div class=""list-group-item"">{0} </div--> <h4 class=""panel-title""><a href=""#collapse" + number + @""" data-toggle=""collapse"" data-parent=""#accordion"" ><h4>[{3}]</h4>{2}  方法名：{1}   ", kv.Value.Item1, kv.Value.Item2.Name.Replace("<", "&lt;").Replace(">", " &gt;"), helptxt, number);
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
                            sb.AppendFormat("     )  Method:POST   返回类型：{0}[自启动运行{1}次][{2}]</a>", Proxy.GetTypeName(kv.Value.Item2.ReturnType).Replace("<", "&lt;").Replace(">", " &gt;"), kv.Value.Item3,kv.Value.Item4);
                            sb.Append(@"</h4></div><div id=""collapse" + number);
                            sb.Append(@""" class=""panel-collapse collapse"">");
                            //   sb.Append(@"<div class=""list-group-item"">Example:Request Body JSON : <font color=red>[");
                            //   for (int xx = 0; xx < para.Length; xx++)
                            //   {
                            //       sb.AppendFormat("{0}", Proxy.GetTypeName(para[xx].ParameterType).Replace("<", "&lt;").Replace(">", " &gt;"));
                            //       if (xx != para.Length - 1)
                            //        {
                            //           sb.Append(",");
                            //       }
                            //   }
                            //   sb.Append(@"]</font></div>");
                            //    sb.AppendFormat(@"<div class=""list-group-item"">API URL<br />[1]:{0}/{1}<br />[2]:{0}/WEBAPI/{2}/{3} </div> ", _url, kv.Key, kv.Value.Item1, kv.Value.Item2.Name);





                            clazzpath = ((UcAsp.RPC.Restful)clazz[0]).Path.ToLower();
                            sb.Append(@"<div class=""list-group-item"">Example:Request Body JSON : <br /><font color=red  style=""word-wrap:break-word"">{");
                            for (int xx = 0; xx < para.Length; xx++)
                            {
                                sb.AppendFormat("\"{0}\":", para[xx].Name);
                                if (para[xx].ParameterType.FullName.ToString().StartsWith("System"))
                                {
                                    sb.AppendFormat(para[xx].ParameterType.Name.ToString());
                                }
                                else
                                {
                                    sb.Append("{");
                                    PropertyInfo[] ps = para[xx].ParameterType.GetProperties();
                                    int pn = 0;
                                    foreach (PropertyInfo p in ps)
                                    {
                                        sb.Append("\"" + p.Name + "\":");
                                        if (p.PropertyType.FullName.ToString().StartsWith("System"))
                                        {
                                            sb.AppendFormat(p.PropertyType.Name.ToString());
                                        }
                                        else
                                        {
                                            sb.AppendFormat("value");
                                        }
                                        if (pn != ps.Length - 1)
                                        {
                                            sb.Append(",");
                                        }
                                        pn++;
                                    }
                                    sb.Append("}");
                                }
                                if (xx != para.Length - 1)
                                {
                                    sb.Append(",");
                                }
                            }
                            sb.Append(@"}</font><br />");//</div><div class=""list-group-item"">
                            sb.AppendFormat(@"API URL<br />[1]:{0}/WEBAPI/{1}/{2}/<br /> </div> ", _url, clazzpath, path);



                            if (null != cattri && cattri.Length > 0)
                            {
                                Restful rf = (Restful)cattri[0];
                                path = rf.Path.ToLower();
                            }
                            clazzpath = ((Restful)clazz[0]).Path.ToLower();
                            sb.Append(@"<div class=""list-group-item"">Example:WebSocket Send Data JSON : <br /><font color=red  style=""word-wrap:break-word"">{""clazz"":""" + clazzpath + @""",""method"":""" + path + @""",""param"": {");
                            for (int xx = 0; xx < para.Length; xx++)
                            {
                                sb.AppendFormat("\"{0}\":", para[xx].Name);
                                if (para[xx].ParameterType.FullName.ToString().StartsWith("System"))
                                {
                                    sb.AppendFormat(para[xx].ParameterType.Name.ToString());
                                }
                                else
                                {
                                    sb.Append("{");
                                    PropertyInfo[] ps = para[xx].ParameterType.GetProperties();
                                    int pn = 0;
                                    foreach (PropertyInfo p in ps)
                                    {
                                        sb.Append("\"" + p.Name + "\":");
                                        if (p.PropertyType.FullName.ToString().StartsWith("System"))
                                        {
                                            sb.AppendFormat(p.PropertyType.Name.ToString());
                                        }
                                        else
                                        {
                                            sb.AppendFormat("value");
                                        }
                                        if (pn != ps.Length - 1)
                                        {
                                            sb.Append(",");
                                        }
                                        pn++;
                                    }
                                    sb.Append("}");
                                }
                                if (xx != para.Length - 1)
                                {
                                    sb.Append(",");
                                }
                            }
                            sb.Append(@"}}</font> <br />");//</div><div class=""list-group-item"">
                            sb.AppendFormat(@"API URL<br />[1]:{0}/websocket/call/<br /> </div> ", _url.Replace("http", "ws"), clazzpath, path);
                            sb.Append(@"<div  class=""list-group-item"" style=""word-wrap:break-word""> 返回数据类型(注：code-100正常，200完成，300超时，500系统错误，400服务不存在，900严重错误)：<br />{""code"":200,""msg"":""成功"",""data"":");
                            if (kv.Value.Item2.ReturnType.FullName.StartsWith("System") && kv.Value.Item2.ReturnType.Name.IndexOf("`") < 0)
                            {
                                sb.AppendFormat(@"{0}", kv.Value.Item2.ReturnType.Name.ToString());
                            }
                            else
                            {
                                if (kv.Value.Item2.ReturnType.FullName.IndexOf("System.Collections") > -1)
                                {
                                    PropertyInfo[] ps = kv.Value.Item2.ReturnType.GetProperties();

                                    PropertyInfo[] items = ps[2].PropertyType.GetProperties();
                                    int pn = 0;
                                    sb.Append("[{");
                                    foreach (PropertyInfo p in items)
                                    {
                                        sb.Append("\"" + p.Name + "\":");
                                        if (p.PropertyType.FullName.ToString().StartsWith("System"))
                                        {
                                            sb.AppendFormat(p.PropertyType.Name.ToString());
                                        }
                                        else
                                        {
                                            sb.AppendFormat("value");
                                        }
                                        if (pn != items.Length - 1)
                                        {
                                            sb.Append(",");
                                        }
                                        pn++;
                                    }
                                    sb.Append("}]");
                                }
                                else
                                {
                                    PropertyInfo[] ps = kv.Value.Item2.ReturnType.GetProperties();
                                    int pn = 0;
                                    sb.Append("{");
                                    foreach (PropertyInfo p in ps)
                                    {
                                        sb.Append("\"" + p.Name + "\":");
                                        if (p.PropertyType.FullName.ToString().StartsWith("System"))
                                        {
                                            sb.AppendFormat(p.PropertyType.Name.ToString());
                                        }
                                        else
                                        {
                                            sb.AppendFormat("value");
                                        }
                                        if (pn != ps.Length - 1)
                                        {
                                            sb.Append(",");
                                        }
                                        pn++;
                                    }
                                    sb.Append("}");
                                }

                            }
                            sb.Append("}</div>");
                            sb.Append(@"</div></div>");
                        }



                    }
                }
                sb.Append("</div>");

                sb.Append(@"<script src =""//libs.baidu.com/jquery/2.1.1/jquery.min.js""></script>");
                sb.Append(@"<script src=""//echarts.baidu.com/echarts2/doc/asset/js/bootstrap.min.js""></script>");
                sb.Append("</body></html>");
                return sb.ToString();

            }
            catch (Exception ex)
            {
                return ex.StackTrace.ToString();
            }
        }

        protected override void OnMessage(object sender, MessageEventArgs e)
        {

            Send(HtmlHelp(""));
        }
        protected override void OnGet(HttpRequestEventArgs ev)
        {

            WebSocketSessionManager session = Sessions;
            KeepSession = true;
            Sessions.KeepClean = true;
            byte[] _buffer = GZipUntil.GetZip(Encoding.UTF8.GetBytes(HtmlHelp("http://" + ev.Request.UserHostName)));
            ev.Response.AddHeader("Content-Encoding", "gzip");
            Cookie cookie = ev.Request.Cookies["SessionId"];
            if (cookie != null) { ApiService sn = (ApiService)Sessions[cookie.Value]; }
            session.KeepClean = true;

            ev.Response.WriteContent(_buffer);
        }
    }
}
