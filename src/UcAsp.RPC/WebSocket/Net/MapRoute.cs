/***************************************************
*创建人:rixiang.yu
*创建时间:2017/7/14 14:36:14
*功能说明:<Function>
*版权所有:<Copyright>
*Frameworkversion:4.6.1
*CLR版本：4.0.30319.42000
***************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UcAsp.WebSocket.Net
{
    public class MapRoute
    {
        public MapRoute(string name, string url, object defaults, object rule)
        {
            this.Name = name;
            this.Url = url;
            this.Defaults = defaults;
            this.Rule = rule;
        }
        public string Name { get; set; }
        public string Url { get; set; }
        public dynamic Defaults { get; set; }
        public dynamic Rule { get; set; }
    }
}
