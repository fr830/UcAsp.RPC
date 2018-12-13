/***************************************************
*创建人:rixiang.yu
*创建时间:2017/3/14 16:28:27
*功能说明:<Function>
*版权所有:<Copyright>
*Frameworkversion:4.0
*CLR版本：4.0.30319.42000
***************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using log4net;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Net.NetworkInformation;

namespace UcAsp.RPC
{
    public class SocketClient : ClientBase
    {
        public override ISerializer Serializer => new ProtoSerializer();
        private static CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
        private readonly ILog _log = LogManager.GetLogger(typeof(SocketClient));
        //  Monitor monitor = new Monitor();
        private System.Timers.Timer timer = new System.Timers.Timer();


        public SocketClient()
        {
            timer.Interval = 5000;
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            CheckServer();
            if (_timeoutTask.Count > 50)
            {
                for (int i = 0; i < 10; i++)
                {
                    _timeoutTask.RemoveAt(0);
                }

            }
        }

        public override void Exit()
        {
            cancelTokenSource.Cancel();
            timer.Stop();

        }
        public override DataEventArgs GetResult(DataEventArgs e, ChannelPool channel)
        {

            try
            {
                if (channel.Client == null || channel.Client.Connected != true)
                {
                    channel.Client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    channel.Client.Connect(channel.IpPoint);
                    channel.Available = true;
                }
                byte[] _sbuffer = e.ToByteArray();
                channel.Client.Send(_sbuffer, _sbuffer.Length, SocketFlags.None);

                ByteBuilder builder = new ByteBuilder(1);

                byte[] _recbuff = new byte[2048];
                int i = channel.Client.Receive(_recbuff, 2048, SocketFlags.None);
                builder.Add(_recbuff.Take(i).ToArray());
                int total = builder.GetInt32(0);
                while (channel.Client.Available > 0)
                {
                    i = channel.Client.Receive(_recbuff, 2048, SocketFlags.None);
                    builder.Add(_recbuff, 0, i);
                }
                while (total > builder.Count)
                {
                    i = channel.Client.Receive(_recbuff, 2048, SocketFlags.None);
                    builder.Add(_recbuff, 0, i);
                }
                if (total == builder.Count)
                {
                    DataEventArgs d = DataEventArgs.Parse(builder);
                    d.StatusCode = StatusCode.Success;
                    Channels[e.CallHashCode].ActiveHash = 0;
                    return d;
                }
                Channels[e.CallHashCode].ActiveHash = 0;
                e.StatusCode = StatusCode.TimeOut;
                return e;
            }
            catch (SocketException se)
            {
                foreach (ChannelPool p in Channels)
                {
                    if (p.IpPoint == channel.IpPoint)
                    {
                        p.Available = false;
                    }
                }
                e.StatusCode = StatusCode.Error;
                return e;
            }

            catch (Exception ex)
            {
                e.StatusCode = StatusCode.Error;
                return e;
            }

        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <param name="channel"></param>
        /// <returns></returns>
        /// 
        public override void Call(object dataev, int i)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            DataEventArgs e = (DataEventArgs)dataev;
            ChannelPool channel = Channels[i];
            try
            {
                e.CallHashCode = i;
                Socket _client = channel.Client;
                if (_client == null || _client.Connected != true)
                {
                    channel.Client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    channel.Client.Connect(channel.IpPoint);
                    channel.Available = true;
                }
                _client = channel.Client;

                byte[] _bf = e.ToByteArray();
                #region 異步
                _client.BeginSend(_bf, 0, _bf.Length, SocketFlags.None, null, null);
                #endregion
                #region 異步接收
                StateObject state = new StateObject();
                RunStateObjects.TryAdd(e.TaskId, state);
                state.WorkSocket = _client;
                state.Builder.ReSet();
                SocketError error;
                _client.BeginReceive(state.Buffer, 0, StateObject.BufferSize, SocketFlags.None, out error, ReceiveCallback, state);
                #endregion
                watch.Stop();
                //monitor.Write(e.TaskId, e.ActionCmd, "...", watch.ElapsedMilliseconds, _bf.LongLength.ToString());
                Channels[i].PingActives = DateTime.Now.Ticks;
            }
            catch (SocketException sex)
            {
                foreach (ChannelPool p in Channels)
                {
                    if (p.IpPoint == channel.IpPoint)
                    {
                        p.Available = false;
                    }

                }
                e.StatusCode = StatusCode.Error;
                lock (ResultTask)
                {
                    e.StatusCode = StatusCode.Serious;
                    DataEventArgs timout = _timeoutTask.FirstOrDefault(o => o.TaskId == e.TaskId);
                    if (timout == null)
                    {
                        ResultTask.AddOrUpdate(e.TaskId, e, (key, value) => value = e);
                    }
                    else
                    {
                        _timeoutTask.Remove(timout);
                    }
                }
                _log.Error(sex);
            }
            catch (Exception ex)
            {
                lock (ResultTask)
                {
                    e.StatusCode = StatusCode.Serious;
                    DataEventArgs timout = _timeoutTask.FirstOrDefault(o => o.TaskId == e.TaskId);
                    if (timout == null)
                    {
                        ResultTask.AddOrUpdate(e.TaskId, e, (key, value) => value = e);
                    }
                    else
                    {
                        _timeoutTask.Remove(timout);
                    }
                }
                _log.Error(ex);
            }
        }


        private void ReceiveCallback(IAsyncResult result)
        {

            StateObject state = (StateObject)result.AsyncState;
            Socket handler = state.WorkSocket;
            try
            {


                int bytesRead = handler.EndReceive(result);
                state.Builder.Add(state.Buffer, 0, bytesRead);
                int total = state.Builder.GetInt32(0);
                if (total == state.Builder.Count)
                {
                    DataEventArgs dex = DataEventArgs.Parse(state.Builder);
                    lock (ResultTask)
                    {
                        state.Builder.ReSet();
                        try
                        {
                            DataEventArgs timout = _timeoutTask.FirstOrDefault(o => o.TaskId == dex.TaskId);
                            if (timout == null)
                            {
                                ResultTask.AddOrUpdate(dex.TaskId, dex, (key, value) => value = dex);
                            }
                        }
                        catch (Exception ex)
                        {
                            ResultTask.AddOrUpdate(dex.TaskId, dex, (key, value) => value = dex);
                        }
                    }
                }
                else
                {
                    handler.BeginReceive(state.Buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
                }
            }

            catch (Exception ex)
            {
                state.Builder.ReSet();
                GC.Collect();
                Console.WriteLine(ex);
                _log.Error(ex);
            }
        }





        public override void CheckServer()
        {
            foreach (ChannelPool channel in Channels)
            {
                if (!channel.Available)
                {
                    try
                    {
                        channel.Client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        channel.Client.Connect(channel.IpPoint);
                        channel.Available = true;
                    }
                    catch (Exception ex)
                    { }
                }
            }
        }
    }

}
