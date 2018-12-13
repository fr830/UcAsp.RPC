/***************************************************
*创建人:rixiang.yu
*创建时间:2017/7/15 15:34:23
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
namespace UcAsp.RPC
{
    public class MethodParam
    {
        public ISerializer _serializer = new ProtoSerializer();
        /// <summary>
        /// 纠正参数的值
        /// Json序列化为List(object)后,object的类型和参数的类型不一致
        /// </summary>
        /// <param name="method">欲调用的目标方法</param>
        /// <param name="parameterValues">传递的参数值</param>
        /// <returns></returns>

        public List<object> CorrectParameters(MethodInfo method, List<object> parameterValues)
        {
            if (parameterValues.Count == method.GetParameters().Length)
            {
                for (int i = 0; i < parameterValues.Count; i++)
                {
                    // 传递的参数值
                    object entity = parameterValues[i];
                    if (entity != null)
                    {
                        // 传递参数的类型
                        Type eType = entity.GetType();

                        Type[] ParameterTypes = new Type[method.GetParameters().Length];
                        for (int x = 0; x < method.GetParameters().Length; x++)
                        {
                            string paratype = method.GetParameters()[x].ParameterType.FullName;
                            if (paratype.EndsWith("&"))
                            {
                                paratype = paratype.Replace("&", "");
                            }
                            ParameterTypes[x] = Type.GetType(paratype);// method.GetParameters()[x].ParameterType;
                            if (ParameterTypes[x] == null)
                            {
                                ParameterTypes[x] = method.GetParameters()[x].ParameterType;
                            }
                        }
                        // 目标方法参数类型
                        Type pType = ParameterTypes[i];
                        // 类型不一致，需要转换类型
                        if (eType.Equals(pType) == false)
                        {
                            // 转换entity的类型
                            string param = this._serializer.ToString(entity);
                            object pValue = this._serializer.ToEntity(param, pType);
                            // 保存参数
                            parameterValues[i] = pValue;
                        }
                    }
                    else
                    { parameterValues[i] = null; }
                }
            }

            return parameterValues;
        }
    }
}
