/***************************************************
*创建人:TecD02
*创建时间:2016/9/2 20:16:44
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
namespace UcAsp.RPC
{
    public class ChannelPool
    {
        public bool Available { get; set; }
        
        public IPEndPoint IpAddress { get; set; }

        public int RunTimes { get; set; }

        public long PingActives { get; set; }
    }
}
