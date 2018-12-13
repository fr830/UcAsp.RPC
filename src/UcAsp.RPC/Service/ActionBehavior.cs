using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using log4net;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace UcAsp.RPC.Service
{
    public class ActionBehavior : IBehavior
    {
        private Dictionary<string, Tuple<string, MethodInfo, int, long>> _memberInfos;
        private bool flagrpc = false;
        public ActionBehavior(Dictionary<string, Tuple<string, MethodInfo, int, long>> member)
        {
            _memberInfos = member;
        }
        private static int taskId = 0;
        private readonly ILog _log = LogManager.GetLogger(typeof(ActionBehavior));
        // private static Monitor monitor = new Monitor();
        //Stopwatch wath = new Stopwatch();
        public static DateTime LastRunTime = DateTime.Now;
        public static string LastError = "";
        public static string LastMethod = "";
        public static string LastParam = "";
        public void Executer(HttpContext context)
        {
            long size = 0;
            byte[] _buffer;
            if (context.Request.Method.ToLower() == "post")
            {
                string content = string.Empty;
                using (StreamReader reader = new StreamReader(context.Request.Body))
                {
                    content = reader.ReadToEnd();

                }
                string rpc = context.Request.Headers["UcAsp.Net_RPC"];
                if (rpc?.ToLower() == "true")
                { flagrpc = true; }
                else
                {
                    flagrpc = false;
                }
                string url = context.Request.Path;
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
                    code = rurl[2];
                }
                bool keeplive = false;

                DataEventArgs ea = new DataEventArgs();

                dynamic parameters = new List<object>();

                try
                {
                    parameters = JsonConvert.DeserializeObject<dynamic>(content);
                    if (parameters == null) parameters = new List<object>();
                    ea = Call(name, methodname, code, parameters, ref keeplive);
                }
                catch (Exception ex)
                {

                    ea.StatusCode = StatusCode.Serious;
                    ea.LastError = ex.Message;
                }

                if (flagrpc)
                {
                    _buffer = GZipUntil.GetZip(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(ea)));

                }
                else
                {
                    if (ea.StatusCode != StatusCode.Success)
                    {
                        _buffer = GZipUntil.GetZip(Encoding.UTF8.GetBytes("{\"code\":" + (int)ea.StatusCode + ",\"msg\":\"" + ea.LastError + "\"}"));

                    }
                    else
                    {
                        _buffer = GZipUntil.GetZip(Encoding.UTF8.GetBytes(ea.Json));

                    }

                }
                //wath.Stop();
                // long milli = wath.ElapsedMilliseconds;
                //monitor.Write(ea.TaskId, name, methodname + "." + code, milli, size.ToString());

                size = _buffer.LongLength;



            }
            else
            {
                _buffer = GZipUntil.GetZip(Encoding.UTF8.GetBytes("{\"code\":404,\"msg\":\"服务不存在\"}"));
                context.Response.StatusCode = 404;
            }
            context.Response.ContentLength = _buffer.Length;
            context.Response.Headers.Add("Content-Encoding", "gzip");
            context.Response.Body.Write(_buffer, 0, _buffer.Length);
        }
        protected DataEventArgs Call(string name, string methodname, string code, dynamic parameters, ref bool keeplive)
        {
            long contentleng = 0;
            LastParam = parameters.ToString();
            _log.Error(parameters.ToString());
            LastRunTime = DateTime.Now;
            LastMethod = methodname;

            DataEventArgs ea = new DataEventArgs();
            taskId++;
            ea.TaskId = taskId;
            if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(code))
            {
                ea.StatusCode = StatusCode.NoExit;
                ea.LastError = "方法不存在";
                ea.Json = "{\"responseCode\":" + (int)ea.StatusCode + ",\"responseMsg\":\"" + ea.LastError + "\"}";
                return ea;
            }
            if (string.IsNullOrEmpty(methodname) && string.IsNullOrEmpty(code))
            {
                ea.StatusCode = StatusCode.NoExit;
                ea.LastError = "方法不存在";
                ea.Json = "{\"responseCode\":" + (int)ea.StatusCode + ",\"responseMsg\":\"" + ea.LastError + "\"}";
                return ea;
            }
            MethodInfo method = null;
            string keyvl = code;
            if (string.IsNullOrEmpty(code))
            {
                #region 地址请求
                try
                {
                    lock (_memberInfos)
                    {

                        foreach (KeyValuePair<string, Tuple<string, MethodInfo, int, long>> kv in _memberInfos)
                        {
                            object[] clazz = kv.Value.Item2.DeclaringType.GetCustomAttributes(typeof(Restful), true);
                            string clazzpath = string.Empty;
                            if (clazz != null && clazz.Length > 0 && ((Restful)clazz[0]).Path != null)
                            {
                                if (((Restful)clazz[0]).Path != null)
                                {
                                    clazzpath = ((Restful)clazz[0]).Path.ToLower();
                                }
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
                                        keeplive = rf.KeepAlive;
                                    }
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
                                        Console.WriteLine(ex.Message);
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
                                        keyvl = kv.Key;
                                        name = kv.Value.Item1;
                                        method = kv.Value.Item2;

                                        break;
                                    }

                                }
                            }
                        }

                    }
                }
                catch (Exception ex)
                {
                    _log.Error(ex);
                    Console.WriteLine(ex);
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
                if (_memberInfos.ContainsKey(code))
                {
                    name = _memberInfos[code].Item1;
                    method = _memberInfos[code].Item2;
                }
                else
                {
                    ea.StatusCode = StatusCode.NoExit;
                    ea.LastError = "方法不存在";
                    ea.Json = "{\"responseCode\":" + (int)ea.StatusCode + ",\"responseMsg\":\"" + ea.LastError + "\"}";
                    return ea;
                }
            }
            if (method == null)
            {
                ea.StatusCode = StatusCode.NoExit;
                ea.LastError = "方法不存在";
                ea.Json = "{\"responseCode\":" + (int)ea.StatusCode + ",\"responseMsg\":\"" + ea.LastError + "\"}";
                return ea;
            }

            try
            {
                parameters = new MethodParam().CorrectParameters(method, parameters);
                object[] arrparam = parameters.ToArray();
                Object bll = ApplicationContext.GetObject(name);
                var result = method.Invoke(bll, arrparam);

                JsonSerializerSettings jsonsetting = new JsonSerializerSettings();
                jsonsetting.Formatting = Formatting.Indented;

                ea.StatusCode = StatusCode.Success;
                ea.Param = new System.Collections.ArrayList();
                if (flagrpc)
                {
                    ea.Binary = new JsonSerializer().ToBinary(result);
                    contentleng = ea.Binary.Buffer.LongLength;
                }
                else
                {
                    string data = JsonConvert.SerializeObject(result, jsonsetting);
                    contentleng = data.Length;
                    ea.Json = "{\"responseCode\":" + (int)StatusCode.Success + ",\"responseMsg\":\"成功\",\"responseData\":" + data + "}";
                }
                for (int i = 0; i < arrparam.Length; i++)
                {
                    try
                    {
                        ea.Param.Add(arrparam[i]);
                    }
                    catch (Exception ex)
                    {
                        Console.Write(ex);
                    }
                }
                Console.WriteLine(ea.TaskId);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                Console.WriteLine(ex);
                ea.LastError = ex.InnerException.Message + "\r\n" + ex.Message;
                ea.StatusCode = StatusCode.Serious;
            }
            _memberInfos[keyvl] = new Tuple<string, MethodInfo, int, long>(_memberInfos[keyvl].Item1, _memberInfos[keyvl].Item2, _memberInfos[keyvl].Item3 + 1, _memberInfos[keyvl].Item3 + contentleng);
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
