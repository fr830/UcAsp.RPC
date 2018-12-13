using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Newtonsoft.Json;
namespace UcAsp.RPC
{
    public class JsonSerializer : ProtoSerializer
    {
        public override Binary ToBinary(object entity)
        {
            if (entity == null)
            {
                return null;
            }
            string strentity = JsonConvert.SerializeObject(entity);

            return new Binary(Encoding.UTF8.GetBytes(strentity));

        }

       
        public override T ToEntity<T>(Binary binary)
        {
            string entiry = Encoding.UTF8.GetString(binary.Buffer);
            return JsonConvert.DeserializeObject<T>(entiry);
        }
    }
}
