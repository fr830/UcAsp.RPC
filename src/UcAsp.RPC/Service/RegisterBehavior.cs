using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace UcAsp.RPC.Service
{
    public class RegisterBehavior : IBehavior
    {
        private List<RegisterInfo> _register;
        public RegisterBehavior(List<RegisterInfo> register)
        {
            _register = register;
        }

        public void Executer(HttpContext context)
        {

            DataEventArgs reg = new DataEventArgs();
            string reginfo = JsonConvert.SerializeObject(_register);
            if (context.Request.Path.ToString().ToLower() == "/register")
            {

                reg.Param = new System.Collections.ArrayList();
                reg.Json = reginfo;
                reg.StatusCode = StatusCode.Success;
            }
            else
            {                
                reg.HttpSessionId = Guid.NewGuid().ToString("N");
                reg.StatusCode = StatusCode.Success;
            }
            byte[] _buffer = GZipUntil.GetZip(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(reg)));

            context.Response.ContentLength = _buffer.Length;
            context.Response.ContentType = "application/json";
            context.Response.Headers.Add("Content-Encoding", "gzip");
            context.Response.Body.Write(_buffer, 0, _buffer.Length);

        }
    }
}
