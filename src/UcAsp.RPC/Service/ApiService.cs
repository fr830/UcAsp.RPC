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
namespace UcAsp.RPC.Service
{
    public class ApiService : WebSocketBehavior
    {

        public Dictionary<string, Tuple<string, MethodInfo, int>> MemberInfos { get; set; }

        protected override void OnConnect(HttpRequestEventArgs ev)
        {

        }
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
                object[] cattri = kv.Value.Item2.GetCustomAttributes(true);
                if (clazz != null && clazz.Length > 0)
                {
                    if (null != cattri && cattri.Length > 0)
                    {
                        Restful rf = (Restful)cattri[0];
                        if (rf.Path != null)
                        {
                            path = rf.Path.ToLower();
                        }
                        if (rf.NoRest)
                            continue;
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



                    if (null != cattri && cattri.Length > 0)
                    {
                        Restful rf = (Restful)cattri[0];
                        if (rf.Path != null)
                        {
                            path = rf.Path.ToLower();
                        }
                    }
                    clazzpath = ((Restful)clazz[0]).Path.ToLower();
                    sb.Append(@"<div class=""list-group-item"">Example:WebSocket Send Data JSON{""clazz"":""" + clazzpath + @""",""method"":""" + path + @""",""param"": {");
                    for (int xx = 0; xx < para.Length; xx++)
                    {
                        sb.AppendFormat("\"{0}\":value", para[xx].Name);
                        if (xx != para.Length - 1)
                        {
                            sb.Append(",");
                        }
                    }
                    sb.Append(@"}}</div>");
                    sb.AppendFormat(@"<div class=""list-group-item"">API URL<br />[1]:{0}/websocket/call/<br /> </div> ", _url.Replace("http", "ws"), clazzpath, path);

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

            WebSocketSessionManager session = Sessions;
            byte[] _buffer = GZipUntil.GetZip(Encoding.UTF8.GetBytes(HtmlHelp("http://" + ev.Request.UserHostName)));
            ev.Response.AddHeader("Content-Encoding", "gzip");
            Cookie cookie = ev.Request.Cookies["SessionId"];
            if (cookie != null) { ApiService sn = (ApiService)Sessions[cookie.Value]; }


            ev.Response.WriteContent(_buffer);
        }
    }
}
