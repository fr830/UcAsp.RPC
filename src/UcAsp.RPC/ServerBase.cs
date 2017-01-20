/***************************************************
*创建人:TecD02
*创建时间:2016/8/17 19:33:47
*功能说明:<Function>
*版权所有:<Copyright>
*Frameworkversion:4.0
*CLR版本：4.0.30319.42000
***************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Net.Sockets;
using log4net;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Diagnostics;
using System.Threading;
namespace UcAsp.RPC
{
    public class ServerBase : IServer
    {
        private readonly ILog _log = LogManager.GetLogger(typeof(ServerBase));
        public ISerializer _serializer = new JsonSerializer();
        public const int buffersize = 1024 * 5;

        public DateTime LastRunTime = DateTime.Now;
        public string LastError = "";
        public string LastMethod = "";
        public string LastParam = "";
        public int Timer = 0;
        private long _endsend = DateTime.Now.Ticks;
        public string Authorization { get; set; }
        public virtual Dictionary<string, Tuple<string, MethodInfo, int>> MemberInfos
        { get; set; }
        public virtual List<RegisterInfo> RegisterInfo { get; set; }

        public bool IsStart
        {
            get;
            set;
        }

        public virtual void StartListen(int port)
        {
            IsStart = true;

        }
        /// <summary>
        /// 纠正参数的值
        /// Json序列化为List(object)后,object的类型和参数的类型不一致
        /// </summary>
        /// <param name="method">欲调用的目标方法</param>
        /// <param name="parameterValues">传递的参数值</param>
        /// <returns></returns>

        public virtual List<object> CorrectParameters(MethodInfo method, List<object> parameterValues)
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
                        Binary bin = this._serializer.ToBinary(entity);
                        object pValue = this._serializer.ToEntity(bin, pType);
                        // 保存参数
                        parameterValues[i] = pValue;
                    }
                }
            }

            return parameterValues;
        }

        public virtual void Call(Socket socket, Object obj)
        {
            Timer++;
            Stopwatch wath = new Stopwatch();
            wath.Start();
            DataEventArgs e = (DataEventArgs)obj;
            LastParam = _serializer.ToString(e);
            LastRunTime = DateTime.Now;
            LastMethod = e.ActionParam;
            if (e.ActionCmd == CallActionCmd.Register.ToString())
            {
                e.Binary = this._serializer.ToBinary(RegisterInfo);
                e.StatusCode = StatusCode.Success;
            }
            else if (e.ActionCmd == CallActionCmd.Ping.ToString())
            {
                e.ActionCmd = CallActionCmd.Pong.ToString();
                e.Binary = this._serializer.ToBinary("Pong");
                e.StatusCode = StatusCode.Success;
            }
            else if (e.ActionCmd == CallActionCmd.Validate.ToString())
            {

                e.HttpSessionId = "";
            }
            else if (e.ActionCmd == CallActionCmd.Call.ToString())
            {
                int p = e.ActionParam.LastIndexOf(".");

                string code = e.ActionParam.Substring(p + 1);
                if (string.IsNullOrEmpty(code))
                {
                    e.StatusCode = StatusCode.Serious;
                }
                else
                {
                    if (MemberInfos.ContainsKey(code))
                    {

                        try
                        {
                            string name = MemberInfos[code].Item1;
                            MemberInfos[code] = new Tuple<string, MethodInfo, int>(MemberInfos[code].Item1, MemberInfos[code].Item2, MemberInfos[code].Item3 + 1);
                            MethodInfo method = MemberInfos[code].Item2;
                            var parameters = this._serializer.ToEntity<List<object>>(e.Binary);
                            if (parameters == null) parameters = new List<object>();
                            parameters = this.CorrectParameters(method, parameters);
                            Object bll = ApplicationContext.GetObject(name);

                            var result = method.Invoke(bll, parameters.ToArray());
                            if (!method.ReturnType.Equals(typeof(void)))
                            {
                                e.Binary = this._serializer.ToBinary(result);
                            }
                            else
                            {
                                e.Binary = null;
                            }

                            e.StatusCode = StatusCode.Success;

                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex);
                            e.LastError = ex.Message;
                            if (ex.InnerException != null)
                            {
                                e.LastError = e.LastError + ex.InnerException.Message;
                                _log.Error(ex.InnerException);
                            }

                            e.StatusCode = StatusCode.Serious;
                            LastError = e.LastError;
                            IsStart = false;

                        }
                    }
                    else
                    {
                        e.LastError = LastError = "服务不存在";
                        _log.Error("服务不存在");
                        e.StatusCode = StatusCode.NoExit;

                    }
                }
            }
            Send(socket, e);
            wath.Stop();
            _log.Info(e.ActionParam + ":" + e.CallHashCode + ":" + e.TaskId + ":" + wath.ElapsedMilliseconds);
        }


        public virtual void Send(Socket socket, DataEventArgs e)
        {
            try
            {

                byte[] _bf = e.ToByteArray();
                int l = _bf.Count() / buffersize;
                for (int i = 0; i < l + 1; i++)
                {
                    int sl = buffersize;
                    if (i == l)
                    {
                        sl = _bf.Length - i * buffersize;
                    }
                    socket.Send(_bf, buffersize * i, sl, SocketFlags.None);
                }
                _endsend = DateTime.Now.Ticks;
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }

        public virtual void Stop()
        { }
    }
}
