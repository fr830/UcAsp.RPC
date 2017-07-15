/***************************************************
*创建人:rixiang.yu
*创建时间:2017/7/14 14:36:58
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
using System.Reflection;
using System.Text.RegularExpressions;
using UcAsp.WebSocket.Server;
namespace UcAsp.WebSocket.Net
{
    public class Route
    {
        Dictionary<string, MapRoute> dic = new Dictionary<string, MapRoute>();
        public void Add(string name, string url, WebSocketServiceHost defaults, object rule)
        {
            dic.Add(name, new MapRoute(name, url, defaults, rule));
        }
        public List<WebSocketServiceHost> Values
        {
            get
            {
                List<WebSocketServiceHost> list = new List<WebSocketServiceHost>();
                foreach (KeyValuePair<string, MapRoute> ky in dic)
                {
                    list.Add(ky.Value.Defaults);
                }

                return list;
            }
        }

        public int Count
        {
            get { return this.Values.Count; }
        }

        public List<string> Keys
        {
            get
            {
                List<string> list = new List<string>();
                foreach (KeyValuePair<string, MapRoute> ky in dic)
                {
                    list.Add(ky.Value.Url);
                }

                return list;
            }
        }
        public bool TryGetValue(string reurl, out WebSocketServiceHost host)
        {
            bool flag = false;
            foreach (KeyValuePair<string, MapRoute> ky in dic)
            {
                string newregurl = ky.Value.Url;
                if (reurl == ky.Value.Url)
                {
                    flag = true;
                    host = ky.Value.Defaults;
                    return flag;
                }
                else
                {
                    if (ky.Value.Rule == null)
                    {
                        continue;
                    }
                    Type type = ky.Value.Rule.GetType();
                    PropertyInfo[] pInfo = type.GetProperties();
                    for (int m = 0; m < pInfo.Length; m++)
                    {
                        PropertyInfo p = pInfo[m];
                        newregurl = newregurl.Replace("{" + p.Name + "}", p.GetValue(ky.Value.Rule));
                    }
                    Match match = Regex.Match(reurl, newregurl);
                    if (match != null)
                    {
                        string result = match.Value;
                        if (result == reurl)
                        {
                            flag = true;
                            host = ky.Value.Defaults;
                            return flag;
                        }

                    }
                }



            }
            host = null;
            return false;
        }

        public void Clear()
        {
            dic.Clear();
        }
        public void Remove(string path)
        {
            dic.Remove(path);
        }
    }
}
