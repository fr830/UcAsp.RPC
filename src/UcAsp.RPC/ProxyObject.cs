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

        public IClient Clients { get; set; }
        public IClient Run
        {
            get
            {
                if (Clients == null)
                    throw new Exception("没有可用的服务器");
                else
                {
                    return Clients;

                }

            }
        }

        public ISerializer Serializer
        {
            get { return _serializer; }
        }
    }
}
