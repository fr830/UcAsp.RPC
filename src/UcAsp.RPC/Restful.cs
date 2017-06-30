/***************************************************
*创建人:rixiang.yu
*创建时间:2017/6/29 16:15:48
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
    public class Restful : Attribute
    {
        public string Method { get; set; }
        public string Path { get; set; }

        public Restful() { }
        public Restful(string method, string path) { this.Method = method; this.Path = path; }
        public Restful(string path)
        {
            this.Path = path;
        }
    }
}
