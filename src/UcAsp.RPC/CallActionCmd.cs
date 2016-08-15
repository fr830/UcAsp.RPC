/***************************************************
*创建人:TecD02
*创建时间:2016/8/1 20:23:43
*功能说明:<Function>
*版权所有:<Copyright>
*Frameworkversion:4.0
*CLR版本：4.0.30319.42000
***************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace  UcAsp.RPC
{
    public enum CallActionCmd
    {
        /// <summary>
        /// 调用方法
        /// </summary>
        Call,

        /// <summary>
        /// 验证
        /// </summary>
        Validate,

        /// <summary>
        /// 调用返回正确
        /// </summary>
        Success,

        /// <summary>
        /// 调用返回错误
        /// </summary>
        Error,

        /// <summary>
        /// 发送心跳
        /// </summary>
        Ping,

        /// <summary>
        /// 回复心跳
        /// </summary>
        Pong,

        /// <summary>
        /// 通知断开连接
        /// </summary>
        Exit,

        /// <summary>
        /// 超时
        /// </summary>
        Timeout,

        /// <summary>
        /// 获取http端口
        /// </summary>
        HttpPort
    }
}
