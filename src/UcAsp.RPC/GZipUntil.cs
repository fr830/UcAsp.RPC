/***************************************************
*创建人:rixiang.yu
*创建时间:2017/7/15 14:32:47
*功能说明:<Function>
*版权所有:<Copyright>
*Frameworkversion:4.0
*CLR版本：4.0.30319.42000
***************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
namespace UcAsp.RPC
{
    public class GZipUntil
    {
        public static byte[] GetZip(byte[] buffer)
        {
            byte[] gizpbytes = null;
            using (MemoryStream cms = new MemoryStream())
            {
                using (GZipStream gzip = new GZipStream(cms, CompressionMode.Compress))
                {
                    //将数据写入基础流，同时会被压缩
                    gzip.Write(buffer, 0, buffer.Length);
                }
                gizpbytes = cms.ToArray();
            }
            return gizpbytes;
        }

        public static string UnZip(Stream stream)
        {
            string result = string.Empty;
            GZipStream gzip = new GZipStream(stream, CompressionMode.Decompress);//解压缩
            using (StreamReader reader = new StreamReader(gzip, Encoding.UTF8))//中文编码处理
            {
                result = reader.ReadToEnd();
            }
            return result;
        }
    }
}
