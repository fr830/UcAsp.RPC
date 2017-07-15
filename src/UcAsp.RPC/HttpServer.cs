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
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using System.Reflection;
using Newtonsoft.Json;
using log4net;
using UcAsp.WebSocket;
using UcAsp.WebSocket.Net;
using UcAsp.WebSocket.Server;
using UcAsp.RPC.Service;
namespace UcAsp.RPC
{
    public class HttpServer : ServerBase, IDisposable
    {

        private readonly ILog _log = LogManager.GetLogger(typeof(HttpServer));
        CancellationTokenSource token = new CancellationTokenSource();
        private WebServer web;
        private string _httpversion;
        private string _url = string.Empty;
        private string _mimetype = string.Empty;

        public override Dictionary<string, Tuple<string, MethodInfo, int>> MemberInfos { get; set; }

        public override void StartListen(int port)
        {
            web = new WebServer("http://localhost:" + port + "/");
            web.DocumentRootPath = System.AppDomain.CurrentDomain.BaseDirectory;
            IPAddress[] iplist = Dns.GetHostAddresses(Dns.GetHostName());
            for (int i = 0; i < iplist.Length; i++)
            {
                web.AddPrefixes("http://" + iplist[i] + ":" + port + "/");
            }
            web.AddWebSocketService<ApiService>("/", () => new ApiService() { MemberInfos = MemberInfos }, null);
            web.AddWebSocketService<ApiService>("/help", () => new ApiService() { MemberInfos = MemberInfos }, null);
            web.AddWebSocketService<RegisterService>("/register", () => new RegisterService() { RegisterInfo = RegisterInfo }, null);
            web.AddWebSocketService<ActionService>("/{md5}", () => new ActionService() { MemberInfos = MemberInfos }, new { md5 = "([a-zA-Z0-9]){32,32}" });

            web.AddWebSocketService<ActionService>("/webapi/{clazz}/{method}", () => new ActionService() { MemberInfos = MemberInfos }, new { clazz = "[a-zA-Z0-9.]*", method = "[a-zA-Z0-9]*" });
            web.AddWebSocketService<ActionService>("/websocket/call", () => new ActionService() { MemberInfos = MemberInfos }, null);

            web.Start();



        }

        public override void Stop()
        {
            try
            {
                base.Stop();
                web.Stop();
                token.Cancel();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

        }

        #region IDisposable Support
        private bool disposedValue = false; // 要检测冗余调用

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    web.Stop();

                }

                // TODO: 释放未托管的资源(未托管的对象)并在以下内容中替代终结器。
                // TODO: 将大型字段设置为 null。

                disposedValue = true;
            }
        }

        // TODO: 仅当以上 Dispose(bool disposing) 拥有用于释放未托管资源的代码时才替代终结器。
        // ~HttpServer() {
        //   // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
        //   Dispose(false);
        // }

        // 添加此代码以正确实现可处置模式。
        void IDisposable.Dispose()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(true);
            // TODO: 如果在以上内容中替代了终结器，则取消注释以下行。
            // GC.SuppressFinalize(this);
        }
        #endregion

    }



}
