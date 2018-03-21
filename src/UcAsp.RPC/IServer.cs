/***************************************************
*创建人:余日祥
*创建时间:2016/8/1 19:27:39
*功能说明:<Function>
*版权所有:<Copyright>
***************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
namespace UcAsp.RPC
{
    public interface IServer
    {
        // Dictionary<string, Type> TypeDic { get; set; }
        Dictionary<string, Tuple<string, MethodInfo, int, long>> MemberInfos { get; set; }
        List<RegisterInfo> RegisterInfo { get; set; }
        // event EventHandler<DataEventArgs> OnReceive;
        void StartListen(int port);
        bool IsStart { get; set; }

        void Stop();
        // void Close(object socket);
    }
}
