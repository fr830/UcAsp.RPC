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
using Newtonsoft.Json.Linq;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using ProtoBuf;
namespace UcAsp.RPC
{
    public class ProtoSerializer : ISerializer
    {
        public virtual T ToEntity<T>(Binary binary)
        {
            if (binary == null || binary.Buffer == null || binary.Buffer.Length == 0)
            {
                return default(T);
            }
            using (MemoryStream ms = new MemoryStream(binary.Buffer))
            {
                // IFormatter formater = new BinaryFormatter();
                ms.Seek(0, SeekOrigin.Begin);
                //  return (T)formater.Deserialize(ms);
                return Serializer.Deserialize<T>(ms);


            }
            // return (T)JsonConvert.DeserializeObject(Encoding.UTF8.GetString(binary.Buffer, 0, binary.Buffer.Length), typeof(T));
            // return BJSON.ToObject<T>(binary.Buffer);

        }

        public virtual object ToEntity(Binary binary, Type type)
        {
            if (binary == null || binary.Buffer == null || binary.Buffer.Length == 0)
            {
                return null;
            }
            using (MemoryStream ms = new MemoryStream())
            {
                ms.Seek(0, SeekOrigin.Begin);
                // IFormatter formater = new BinaryFormatter();
                ms.Write(binary.Buffer, 0, binary.Buffer.Length);
                //  return formater.Deserialize(ms);
                return Serializer.Deserialize(type, ms);

            }
            // return JsonConvert.DeserializeObject(Encoding.UTF8.GetString(binary.Buffer, 0, binary.Buffer.Length), type);
            //  return BJSON.ToObject(binary.Buffer);

        }

        public virtual Binary ToBinary(object entity)
        {
            if (entity == null)
            {
                return null;
            }

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Seek(0, SeekOrigin.Begin);
                //  IFormatter formater = new BinaryFormatter();
                // formater.Serialize(ms, entity);
                Serializer.Serialize(ms, entity);
                return new Binary(ms.ToArray());
            }
            //  return  new Binary(fastBinaryJSON.BJSON.ToBJSON(entity));
            // String s = JsonConvert.SerializeObject(entity);
            //return new Binary(Encoding.UTF8.GetBytes(s));


        }

        public virtual Binary ToBinary(object entity, Type type)
        {

            if (entity == null)
            {
                return null;
            }

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Seek(0, SeekOrigin.Begin);
                //  IFormatter formater = new BinaryFormatter();
                // formater.Serialize(ms, entity);
                Serializer.Serialize(ms, entity);
                return new Binary(ms.ToArray());
            }
        }

        public virtual string ToString(object entity)
        {
            return JsonConvert.SerializeObject(entity);
        }

        public virtual T ToEntity<T>(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
               

                return default(T);
            }
        }
        public virtual object ToEntity(string json, Type type)
        {
            return JsonConvert.DeserializeObject(json, type);
        }

    }
}
