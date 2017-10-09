/***************************************************
*创建人:TecD02
*创建时间:2016/8/22 19:03:45
*功能说明:<Function>
*版权所有:<Copyright>
*Frameworkversion:4.0
*CLR版本：4.0.30319.42000
***************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UcAsp.RPC.ProtoBuf;
namespace UcAsp.RPC
{
    [Serializable]
   [ProtoContract]
    public class RegisterInfo
    {
        [ProtoMember(1)]
        public string InterfaceName { get; set; }
        [ProtoMember(2)]
        public string FaceNameSpace { get; set; }
        [ProtoMember(3)]
        public string NameSpace { get; set; }
        [ProtoMember(4)]
        public string ClassName { get; set; }

    }
}
