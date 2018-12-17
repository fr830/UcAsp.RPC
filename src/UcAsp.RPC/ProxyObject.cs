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
using log4net;

namespace UcAsp.RPC
{
    public class ProxyObject
    {
        private readonly static ILog _log = LogManager.GetLogger(typeof(ProxyObject));
        public void Error(DataEventArgs dataEvent)
        {
            _log.Error(Client.RunTime[dataEvent.TaskId]);
            Console.WriteLine(Client.RunTime[dataEvent.TaskId]);
        }
        public IClient Client { get; set; }
        public Binary GetBinary(List<object> entity)
        {
            if (Client.GetType() == typeof(SocketClient))
            {
                return Client.Serializer.ToBinary(Client.Serializer.ToString(entity));
            }
            else
            {
                return Client.Serializer.ToBinary(entity);

            }

        }

    }
}
