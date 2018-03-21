/***************************************************
*创建人:rixiang.yu
*创建时间:2017/3/15 8:24:25
*功能说明:<Function>
*版权所有:<Copyright>
*Frameworkversion:4.0
*CLR版本：4.0.30319.42000
***************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
namespace UcAsp.RPC
{
    public class StateObject
    {
        public Socket WorkSocket = null;

        public const int BufferSize = 1024;

        public byte[] Buffer = new byte[BufferSize];

        public ByteBuilder Builder = new ByteBuilder(1);
    }
}
