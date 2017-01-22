/***************************************************
*创建人:余日祥
*创建时间:2016/8/1 20:27:59
*功能说明:<Function>
*版权所有:<Copyright>
***************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UcAsp.RPC
{
    /// <summary>
    /// 序列化接口
    /// </summary>
    public interface ISerializer
    {
        /// <summary>
        /// 反序列化为实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="binary">数据</param>
        /// <returns></returns>
        T ToEntity<T>(Binary binary);
        /// <summary>
        /// 反序列化为实体
        /// </summary>
        /// <param name="binary">数据</param>
        /// <param name="type">实体类型</param>
        /// <returns></returns>
        object ToEntity(Binary binary, Type type);
        /// <summary>
        /// 序列化为二进制
        /// </summary>
        /// <param name="entity">实体</param>
        /// <returns></returns>
        Binary ToBinary(object entity);

        string ToString(object entity);

        object ToEntity(string json, Type type);

        T ToEntity<T>(string json);
    }
}
