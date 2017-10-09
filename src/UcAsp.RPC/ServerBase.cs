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
  

        public virtual void Call(Socket socket, Object obj)
        {
            Timer++;
            Stopwatch wath = new Stopwatch();
            wath.Start();
            DataEventArgs e = (DataEventArgs)obj;
            Console.WriteLine(e.TaskId);
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

                e.HttpSessionId =  Guid.NewGuid().ToString("N");
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
                            string param = this._serializer.ToEntity<string>(e.Binary);
                            var parameters = this._serializer.ToEntity<List<object>>(param);
                            if (parameters == null) parameters = new List<object>();
                            parameters = new MethodParam().CorrectParameters(method, parameters);
                            Object bll = ApplicationContext.GetObject(name);
                            object[] arrparam = parameters.ToArray();
                            var result = method.Invoke(bll, arrparam);
                            if (!method.ReturnType.Equals(typeof(void)))
                            {
                                e.Binary = this._serializer.ToBinary(result);
                                if (e.Param == null)
                                {
                                    e.Param = new System.Collections.ArrayList();
                                }
                                for (int i = 0; i < arrparam.Length; i++)
                                {                                    
                                    e.Param.Add(arrparam[i]);
                                }
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
                            Console.WriteLine(ex);
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
                socket.BeginSend(_bf, 0, _bf.Length, SocketFlags.None, null, null);

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
