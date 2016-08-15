/***************************************************
*创建人:TecD02
*创建时间:2016/7/30 18:52:56
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
{   /// <summary>
    /// 二进制数据
    /// </summary>
    /// 
    [Serializable]
    public class Binary
    {
        /// <summary>
        /// 基础数据
        /// </summary>
        public byte[] Buffer { get; set; }

        /// <summary>
        /// 二进制数据
        /// </summary>
        /// <param name="buffer">数据</param>
        public Binary(byte[] buffer)
        {
            this.Buffer = buffer;
        }
    }
}
