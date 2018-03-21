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
using System.Diagnostics;
using Newtonsoft.Json;
using System.IO.Compression;
namespace UcAsp.RPC
{
    public class HttpClient : ClientBase
    {
        private ISerializer _serializer = new JsonSerializer();
        private static CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
        private readonly ILog _log = LogManager.GetLogger(typeof(TcpClient));
        private const int buffersize = 1024 * 5;
        private System.Timers.Timer heatbeat = new System.Timers.Timer();
        private Monitor monitr = new Monitor();



        private void Heatbeat_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            CheckServer();
        }

        public override void Exit()
        {
            cancelTokenSource.Cancel();
        }
        public override void Call(object obj, int len)
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
                    dynamic dyjs = JsonConvert.DeserializeObject<dynamic>(redata.Json);

                    ea.Json = dyjs.data.ToString();

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
                        CheckServer();

                    }
                    else
                    {
                        ea.Json = result.Item2;
                        ResultTask.AddOrUpdate(ea.TaskId, ea, (key, value) => value = ea);
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
        /*     public override bool Connect(string ip, int port, int pool)
             {
                 bool flag = true;
                 for (int i = 0; i < pool; i++)
                 {
                     try
                     {
                         IPEndPoint ep = new IPEndPoint(IPAddress.Parse(ip), port);
                         ChannelPool channel = new ChannelPool() { Available = true, IpPoint = ep, PingActives = 0, RunTimes = 0 };
                         IpAddress.Add(channel);

                     }
                     catch (Exception ex)
                     {
                         flag = false;
                         _log.Error(ex);
                     }
                 }
                 return flag;
             }
             */
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

        public override DataEventArgs GetResult(DataEventArgs e, ChannelPool channel)
        {
            string url = "http://" + channel.IpPoint.Address.ToString() + ":" + (channel.IpPoint.Port + 1);
            Dictionary<string, string> header = new Dictionary<string, string>();
            header.Add("Authorization", "Basic " + this.Authorization);
            Tuple<HttpStatusCode, string> result = HttpPost.Post(url + "/" + e.ActionCmd.ToString(), null, header);
            DataEventArgs redata = JsonConvert.DeserializeObject<DataEventArgs>(result.Item2);
            if (result.Item1 == HttpStatusCode.OK)
            {
                redata.StatusCode = StatusCode.Success;
                redata.Json = redata.Json;
                redata.Param = redata.Param;
                return redata;
            }
            else
            {
                e.StatusCode = StatusCode.Error;
                e.Json = result.Item2;
                return e;
            }

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
                    string responseText = string.Empty;
                    GZipStream gzip = new GZipStream(response.GetResponseStream(), CompressionMode.Decompress);//解压缩
                    using (StreamReader reader = new StreamReader(gzip, Encoding.UTF8))//中文编码处理
                    {
                        responseText = reader.ReadToEnd();
                    }

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
