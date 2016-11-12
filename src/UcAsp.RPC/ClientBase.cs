/***************************************************
*创建人:TecD02
*创建时间:2016/11/11 22:33:20
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
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Concurrent;
using log4net;

namespace UcAsp.RPC
{
    public class ClientBase : IClient
    {

        private Socket socket;
        public List<ChannelPool> IpAddress { get; set; }


        public virtual string LastError
        {
            get;

            set;
        }

        public virtual DataEventArgs CallServiceMethod(object e)
        {

            return (DataEventArgs)e;
        }


        public virtual void Connect(string ip, int port, int pool)
        {


        }




        public virtual void Exit()
        {
            // Dispose();
        }

    }
}
