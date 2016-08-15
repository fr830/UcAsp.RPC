/***************************************************
*创建人:TecD02
*创建时间:2016/8/1 20:28:34
*功能说明:<Function>
*版权所有:<Copyright>
*Frameworkversion:4.0
*CLR版本：4.0.30319.42000
***************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
namespace UcAsp.RPC
{
    public class JsonSerializer:ISerializer
    {
        

        public T ToEntity<T>(Binary binary)
        {
            if (binary == null || binary.Buffer == null || binary.Buffer.Length == 0)
            {
                return default(T);
            }
            String s = Encoding.UTF8.GetString(binary.Buffer, 0, binary.Buffer.Length);
            return JsonConvert.DeserializeObject<T>(s);
        }

        public object ToEntity(Binary binary, Type type)
        {
            if (binary == null || binary.Buffer == null || binary.Buffer.Length == 0)
            {
                return null;
            }
            String s = Encoding.UTF8.GetString(binary.Buffer, 0, binary.Buffer.Length);
            return JsonConvert.DeserializeObject(s, type);
        }

        public Binary ToBinary(object entity)
        {
            if (entity == null)
            {
                return null;
            }

            String s = JsonConvert.SerializeObject(entity);
            return new Binary(Encoding.UTF8.GetBytes(s));
        }
    }
}
