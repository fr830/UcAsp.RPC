/***************************************************
*创建人:TecD02
*创建时间:2016/12/21 10:45:31
*功能说明:<Function>
*版权所有:<Copyright>
*Frameworkversion:4.0
*CLR版本：4.0.30319.42000
***************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UcAsp.RPC
{
    public class ServerPort
    {
        public string Ip { get; set; }
        public int Port { get; set; }

        public int Pool { get; set; }
    }
}
