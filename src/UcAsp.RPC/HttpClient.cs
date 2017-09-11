/***************************************************
*创建人:TecD02
*创建时间:2017/1/20 16:44:56
*功能说明:<Function>
*版权所有:<Copyright>
*Frameworkversion:4.0
*CLR版本：4.0.30319.42000
***************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Concurrent;
using log4net;
using System.IO.Compression;
using System.Diagnostics;
using Newtonsoft.Json;
namespace UcAsp.RPC
{
    public class HttpClient : ClientBase
    {
        private ISerializer _serializer = new JsonSerializer();
        private static CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
        private readonly ILog _log = LogManager.GetLogger(typeof(TcpClient));
        private const int buffersize = 1024 * 5;
        private System.Timers.Timer heatbeat = new System.Timers.Timer();

        public override void CallServiceMethod(DataEventArgs e)
        {
            lock (ClientTask)
            {

                if (ApplicationContext._taskId < int.MaxValue)
                {
                    ApplicationContext._taskId++;
                }
                else
                {
                    ApplicationContext._taskId = 0;
                }
                e.TaskId = ApplicationContext._taskId;
                ClientTask.Enqueue(e);
            }
        }

        public override DataEventArgs GetResult(DataEventArgs e)
        {
            int time = 0;
            Task<DataEventArgs> chektask = new Task<DataEventArgs>(() =>
            {
                while (true)
                {
                    if (cancelTokenSource.IsCancellationRequested)
                    {
                        return null;
                    }
                    Thread.Sleep(5);
                    DataEventArgs er = new DataEventArgs();
                    bool result = ResultTask.TryGetValue(e.TaskId, out er);
                    if (result)
                    {
                        return er;
                    }
                    if (time > 10000)
                    {
                        if (e.TryTimes < 6)
                        {
                            e.TryTimes++;
                            ClientTask.Enqueue(e);
                        }
                        else
                        {
                            e.StatusCode = StatusCode.TimeOut;
                            return e;
                        }
                    }
                    time++;

                }
            }, cancelTokenSource.Token);
            chektask.Start();
            DataEventArgs data = chektask.Result;

            return data;
        }

        public override void Run()
        {
            heatbeat.Interval = 30000;
            heatbeat.Elapsed -= Heatbeat_Elapsed;
            heatbeat.Elapsed += Heatbeat_Elapsed;
            heatbeat.Start();
            Task sendtask = new Task(() =>
            {
                while (!cancelTokenSource.IsCancellationRequested)
                {
                    Thread.Sleep(2);
                    DataEventArgs e;
                    if (ClientTask.TryDequeue(out e))
                    {
                        try
                        {
                            int i = new Random(e.GetHashCode()).Next(Channels.Count);
                            ChannelPool channel = Channels[i];
                            int times = 0;
                            while (channel.ActiveHash == 1 && times < 300)
                            {
                                i = new Random(e.GetHashCode()).Next(Channels.Count);
                                channel = Channels[i];
                                Thread.Sleep(1);
                                times++;
                            }
                            Channels[i].ActiveHash = e.TaskId;
                            Channels[i].RunTimes++;
                            Channels[i].PingActives = DateTime.Now.Ticks;
                            Call(e, i);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                            _log.Error(ex);
                        }

                    }

                }

            }, cancelTokenSource.Token);
            sendtask.Start();
        }

        private void Heatbeat_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            //CheckServer();
        }

        public override void Exit()
        {
            cancelTokenSource.Cancel();
        }
        private void Call(object obj, int len)
        {
            DataEventArgs outDea = new DataEventArgs();
            DataEventArgs ea = (DataEventArgs)obj;
            try
            {
                if (RuningTask == null)
                {
                    RuningTask = new ConcurrentDictionary<int, DataEventArgs>();
                }
                RuningTask.AddOrUpdate(ea.TaskId, ea, (key, value) => value = ea);
                string url = "http://" + Channels[len].IpPoint.Address.ToString() + ":" + (Channels[len].IpPoint.Port + 1);
                Dictionary<string, string> header = new Dictionary<string, string>();
                header.Add("Authorization", "Basic " + this.Authorization);
                if (ea.ActionCmd == CallActionCmd.Register.ToString())
                {

                    Tuple<HttpStatusCode, string> result = HttpPost.Post(url + "/" + CallActionCmd.Register.ToString(), null, header);
                    DataEventArgs redata = JsonConvert.DeserializeObject<DataEventArgs>(result.Item2);
                    if (result.Item1 == HttpStatusCode.OK)
                    {
                        ea.StatusCode = StatusCode.Success;
                        dynamic data = JsonConvert.DeserializeObject<dynamic>(redata.Json);
                        ea.Json = data.data.ToString();
                        ea.Param = redata.Param;
                        ResultTask.AddOrUpdate(ea.TaskId, ea, (key, value) => value = ea);
                    }
                    else
                    {
                        ea.StatusCode = StatusCode.Error;
                        if (HttpStatusCode.Moved == result.Item1)
                        {
                            ea.Json = result.Item2;
                            RuningTask.TryRemove(ea.TaskId, out outDea);
                            Channels[len].Available = false;
                            ClientTask.Enqueue(ea);
                         
                        }
                        else
                        {
                            ea.Json = result.Item2;
                            ResultTask.AddOrUpdate(ea.TaskId, ea, (key, value) => value = ea);
                        }
                    }

                }
                else
                {
                    int p = ea.ActionParam.LastIndexOf(".");
                    List<object> eabinary = _serializer.ToEntity<List<object>>(ea.Binary);
                    string code = ea.ActionParam.Substring(p + 1);
                    Dictionary<string, string> param = new Dictionary<string, string>();
                    param.Add("", JsonConvert.SerializeObject(eabinary));
                    var result = HttpPost.Post(url + "/" + code, param, header);
                    DataEventArgs redata = JsonConvert.DeserializeObject<DataEventArgs>(result.Item2);
                    if (result.Item1 == HttpStatusCode.OK)
                    {
                        ea.StatusCode = StatusCode.Success;
                        ea.Json = redata.Json;
                        ea.Param = redata.Param;
                        ResultTask.AddOrUpdate(ea.TaskId, ea, (key, value) => value = ea);
                    }
                    else
                    {
                        ea.StatusCode = StatusCode.Error;
                        if (HttpStatusCode.Moved == result.Item1)
                        {
                            ea.Json = result.Item2;
                            RuningTask.TryRemove(ea.TaskId, out outDea);
                            Channels[len].Available = false;
                            ClientTask.Enqueue(ea);

                        }
                        else
                        {
                            ea.Json = result.Item2;
                            ResultTask.AddOrUpdate(ea.TaskId, ea, (key, value) => value = ea);
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                _log.Error(ex + ex.StackTrace);
                ea.StatusCode = StatusCode.TimeOut;
                ea.TryTimes++;
                if (ea.TryTimes < 3)
                {
                    RuningTask.TryRemove(ea.TaskId, out outDea);
                    ClientTask.Enqueue(ea);
                    return;
                }

                ResultTask.AddOrUpdate(ea.TaskId, ea, (key, value) => value = ea);
                return;
            }
            finally
            {
                for (int i = 0; i < Channels.Count; i++)
                {
                    if (Channels[i].ActiveHash == ea.TaskId)
                    {
                        Channels[i].ActiveHash = 0;
                    }
                }
                RuningTask.TryRemove(ea.TaskId, out outDea);
            }
        }

        public override DataEventArgs GetResult(DataEventArgs e, ChannelPool channel)
        {
            throw new NotImplementedException();
        }
        public override void CheckServer()
        {
            Task t = new Task(() =>
            {
                if (heatbeat.Enabled)
                {
                    heatbeat.Stop();
                    heatbeat.Enabled = false;
                    try
                    {
                        for (int i = 0; i < Channels.Count; i++)
                        {

                            string url = "http://" + Channels[i].IpPoint.Address.ToString() + ":" + (Channels[i].IpPoint.Port + 1);
                            if (HttpPost.CheckHttp(url) == HttpStatusCode.OK)
                            {
                                Channels[i].Available = true;
                            }
                            else
                            {
                                Channels[i].Available = false;
                            }

                        }
                    }
                    finally
                    {
                        heatbeat.Start();
                        heatbeat.Enabled = true;
                    }
                }

            });

            t.Start();

        }

        protected internal class HttpPost
        {
            public static Tuple<HttpStatusCode, string> Post(string url, Dictionary<string, string> param, Dictionary<string, string> header)
            {
                Stopwatch Watch = new Stopwatch();
                Watch.Start();
                try
                {

                    System.Net.HttpWebRequest request = (System.Net.HttpWebRequest)WebRequest.Create(url);
                    request.Method = "POST";
                    request.Headers.Add("UcAsp.Net_RPC", "true");
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.KeepAlive = false;
                    request.Timeout = 1000 * 30;
                    request.ServicePoint.Expect100Continue = false;
                    if (header != null)
                    {
                        foreach (KeyValuePair<string, string> kv in header)
                        {
                            request.Headers[kv.Key] = kv.Value;
                        }
                    }
                    StringBuilder sb = new StringBuilder();
                    if (param != null)
                    {
                        int i = 0;
                        if (param.ContainsKey(""))
                        {
                            sb.Append(param[""]);
                        }
                        else
                        {
                            foreach (KeyValuePair<string, string> kv in param)
                            {
                                if (i == 0)
                                {
                                    sb.AppendFormat("{0}={1}", kv.Key, kv.Value);
                                }
                                else
                                {
                                    sb.AppendFormat("&{0}={1}", kv.Key, kv.Value);
                                }
                            }
                        }
                    }

                    byte[] payload = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
                    request.ContentLength = payload.Length;
                    System.IO.Stream writer = request.GetRequestStream();
                    writer.Write(payload, 0, payload.Length);
                    System.Net.HttpWebResponse response = (System.Net.HttpWebResponse)request.GetResponse();
                    string responseText = GZipUntil.UnZip(response.GetResponseStream());
                    writer.Close();

                    return new Tuple<HttpStatusCode, string>(response.StatusCode, responseText);

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    return new Tuple<HttpStatusCode, string>(HttpStatusCode.Moved, "{\"msg\":\"" + ex.Message + "\"}");
                }
                finally
                {

                    Watch.Stop();
                }
            }

            public static HttpStatusCode CheckHttp(string url)
            {
                try
                {
                    System.Net.HttpWebRequest request = (System.Net.HttpWebRequest)WebRequest.Create(url);
                    System.Net.HttpWebResponse response = (System.Net.HttpWebResponse)request.GetResponse();
                    return response.StatusCode;
                }
                catch (Exception ex)
                {
                    return HttpStatusCode.Moved;
                }
            }
        }
    }
}
