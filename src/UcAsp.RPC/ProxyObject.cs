/***************************************************
*创建人:TecD02
*创建时间:2016/8/2 15:07:25
*功能说明:<Function>
*版权所有:<Copyright>
*Frameworkversion:4.0
*CLR版本：4.0.30319.42000
***************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UcAsp.RPC;
using Newtonsoft.Json;
namespace UcAsp.RPC
{
    public class ProxyObject
    {
        private ISerializer _serializer = new JsonSerializer();

        private IClient _client;


        public IClient Client
        {
            get { return this._client;  }
            set { this._client = value; }
        }

        public ISerializer Serializer
        {
            get { return _serializer; }
        }
    }
}
