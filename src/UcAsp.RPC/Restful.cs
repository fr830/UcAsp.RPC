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
        /// <summary>
        /// 是否出现在接口中
        /// </summary>
        public bool NoRest { get; set; }

        public bool KeepAlive { get; set; }
        public Restful()
        { }
        public Restful(bool noRest)
        {
            this.NoRest = noRest;
        }
        public Restful(string method, string path) : this(path)
        {
            this.Method = method;
        }
        public Restful(string method, string path, bool noRest) : this(method, path)
        {
            this.NoRest = noRest;
        }
        public Restful(string method, string path, bool noRest, bool keepAlive) : this(method, path, noRest)
        {
            this.KeepAlive = keepAlive;
        }

        public Restful(string path)
        {
            if (path == "register")
            {
                Exception ex = new Exception("register路径是系统保留，请更换！");
                throw (ex);
            }

            this.Path = path;
        }
    }
}
