/***************************************************
*创建人:TecD02
*创建时间:2016/11/11 17:51:29
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
{
    public enum StatusCode
    {
        /// <summary>
        /// 正常
        /// </summary>
        Normal = 100,
        /// <summary>
        /// 完成
        /// </summary>
        Success = 200,
        /// <summary>
        /// 超时
        /// </summary>
        TimeOut = 300,

        /// <summary>
        /// 错误
        /// </summary>
        Error = 500,
        /// <summary>
        /// 服务不存在
        /// </summary>
        NoExit = 400,
        /// <summary>
        /// 严重错误
        /// </summary>
        Serious = 900


    }
}
