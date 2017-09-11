/***************************************************
*创建人:rixiang.yu
*创建时间:2017/9/11 13:11:44
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
using UcAsp.WebSocket;
using System.IO;

namespace UcAsp.RPC
{
    public class Monitor
    {
        private static Logger log;
        private static string filename = DateTime.Now.Year + DateTime.Now.Month.ToString("00") + DateTime.Now.Day.ToString("00");
        private static string path = AppDomain.CurrentDomain.BaseDirectory + "log\\";
        private static bool flagmonitor = false;
        public Monitor()
        {

            string _config = AppDomain.CurrentDomain.BaseDirectory + "Application.config";
            if (!File.Exists(_config))
            {
                _config = AppDomain.CurrentDomain.BaseDirectory + "wcs.config";
            }
            if (!File.Exists(_config))
            {
                _config = AppDomain.CurrentDomain.BaseDirectory + "iscs.config";
            }
            Config config = new Config(_config) { GroupName = "service" };
            object monitor = config.GetValue("server", "monitor");
            if (monitor == null)
            {
                config.GroupName = "client";
                monitor = config.GetValue("server", "monitor");
            }
            if (monitor != null)
            {
                flagmonitor = Convert.ToBoolean(monitor);
            }
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string filename = DateTime.Now.Year + DateTime.Now.Month.ToString("00") + DateTime.Now.Day.ToString("00");
            if (log == null)
            {
                log = new Logger(LogLevel.Monitor);
                log.File = path + filename + ".rpt";
            }
        }

        public void Write(long taskid, string spacename, string method, long milli)
        {

            if (flagmonitor)
            {
                string _pre = DateTime.Now.Year + DateTime.Now.Month.ToString("00") + DateTime.Now.Day.ToString("00");
                if (_pre != filename)
                {
                    filename = _pre;
                    log.File = path + filename + ".rpt";
                }

                log.Monitor(taskid + "    " + spacename + " " + method + "  " + (milli).ToString());

            }
        }
    }
}
