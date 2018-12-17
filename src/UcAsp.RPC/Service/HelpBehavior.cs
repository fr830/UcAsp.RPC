using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;

namespace UcAsp.RPC.Service
{
    public class HelpBehavior : IBehavior
    {
        private Dictionary<string, Tuple<string, MethodInfo, int, long>> _memberInfos;
        public HelpBehavior(Dictionary<string, Tuple<string, MethodInfo, int, long>> member)
        {
            _memberInfos = member;
        }

        public void Executer(HttpContext context)
        {
            byte[] buffer = GZipUntil.GetZip(Encoding.UTF8.GetBytes(HtmlHelp("http://" + context.Request.Host.ToString())));
            context.Response.ContentLength = buffer.Length;
            context.Response.Headers.Add("Content-Encoding", "gzip");
            context.Response.Body.Write(buffer, 0, buffer.Length);
        }
        private string HtmlHelp(string _url)
        {

            try
            {
                int number = 0;
                Dictionary<string, Tuple<string, MethodInfo, int, long>> dic = new Dictionary<string, Tuple<string, MethodInfo, int, long>>(_memberInfos);
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
                    foreach (KeyValuePair<string, Tuple<string, MethodInfo, int, long>> kv in dic.OrderBy(o => o.Value.Item1))
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
                            sb.AppendFormat("     )  Method:POST   返回类型：{0}[自启动运行{1}次][{2}]</a>", Proxy.GetTypeName(kv.Value.Item2.ReturnType).Replace("<", "&lt;").Replace(">", " &gt;"), kv.Value.Item3, kv.Value.Item4);
                            sb.Append(@"</h4></div><div id=""collapse" + number);
                            sb.Append(@""" class=""panel-collapse collapse"">");




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
                Console.WriteLine(ex);
                return ex.StackTrace.ToString();
            }
        }


    }
}
