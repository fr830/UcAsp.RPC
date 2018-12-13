using System;
using System.Collections.Generic;
using System.Text;

namespace UcAsp.RPC.Service
{
    public class MapRoute
    {
        public MapRoute(string name, string url, IBehavior defaults, object rule)
        {
            this.Name = name;
            this.Url = url;
            this.Defaults = defaults;
            this.Rule = rule;
        }
        public string Name { get; set; }
        public string Url { get; set; }
        public IBehavior Defaults { get; set; }
        public dynamic Rule { get; set; }
    }
}
