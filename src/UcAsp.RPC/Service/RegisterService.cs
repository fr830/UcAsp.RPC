/***************************************************
*创建人:rixiang.yu
*创建时间:2017/7/15 18:24:31
*功能说明:<Function>
*版权所有:<Copyright>
*Frameworkversion:4.0
*CLR版本：4.0.30319.42000
***************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UcAsp.WebSocket.Server;
using UcAsp.WebSocket.Net;
using UcAsp.WebSocket;
using Newtonsoft.Json;
namespace UcAsp.RPC.Service
{
    public class RegisterService : WebSocketBehavior
    {
        public List<RegisterInfo> RegisterInfo { get; set; }
        protected override void OnGet(HttpRequestEventArgs ev)
        {
            Send(ev);
        }
        protected override void OnPost(HttpRequestEventArgs ev)
        {
            Send(ev);
        }

        private void Send(HttpRequestEventArgs ev)
        {
            string reginfo = JsonConvert.SerializeObject(RegisterInfo);
            DataEventArgs reg = new DataEventArgs();
            reg.Param = new System.Collections.ArrayList();
            if (ev.Request.RawUrl.ToString().ToLower() == "/register")
            {

                reg.Json = reginfo;
                reg.StatusCode = StatusCode.Success;
                byte[] _buffer = GZipUntil.GetZip(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(reg)));
                ev.Response.AddHeader("Content-Encoding", "gzip");
                ev.Response.WriteContent(_buffer);
            }
            else
            {
                reg.HttpSessionId = Guid.NewGuid().ToString("N");
                reg.StatusCode = StatusCode.Success;
                byte[] _buffer = GZipUntil.GetZip(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(reg)));
                ev.Response.AddHeader("Content-Encoding", "gzip");
                ev.Response.WriteContent(_buffer);
            }
        }
    }
}
