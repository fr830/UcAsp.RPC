/***************************************************
*创建人:TecD02
*创建时间:2016/7/30 18:49:39
*功能说明:<Function>
*版权所有:<Copyright>
*Frameworkversion:4.0
*CLR版本：4.0.30319.42000
***************************************************/
using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using log4net;
namespace UcAsp.RPC
{
    [Serializable]
    public class DataEventArgs : EventArgs
    {
        private readonly static ILog _log = LogManager.GetLogger(typeof(DataEventArgs));
        /// 二进制数据
        /// </summary>
        public Binary Binary { get; set; }

        public int CallHashCode { get; set; }

        public String ActionCmd { get; set; }

        public String ActionParam { get; set; }

        public String HttpSessionId { get; set; }

        public int? TaskId { get; set; }

        // 无数据时包的固定长度 24
        //总包长4 + ActionCmd长 4 + ActionParam长 4 + SessionId长 4 + hashCode 4 + 验校总包长 4
        //数据格式   [包长 4byte][ActionCmd长 4byte][ActionParam长 4byte][SessionId长 4byte][hashCode 4byte][ActionCmd N个byte][ActionParam N个byte][SessionId N个byte][实体 N个byte][包长 4byte]
        private const int ConstLength = 24;

        public DataEventArgs()
        {
            this.CallHashCode = 0;
            this.ActionCmd = "";
            this.ActionParam = "";
            this.HttpSessionId = "";

        }
        public int TryTimes { get; set; }
        /// <summary>
        /// 转换为二进制数据
        /// </summary>
        /// <returns></returns>
        public byte[] ToByteArray()
        {
            int cmdLength = this.ActionCmd.Length;
            int paramLength = Encoding.UTF8.GetByteCount(this.ActionParam);
            int idLength = Encoding.UTF8.GetByteCount(HttpSessionId);
            int capacity = ConstLength + cmdLength + paramLength + idLength;

            if (this.Binary != null && this.Binary.Buffer != null)
            {
                capacity = capacity + this.Binary.Buffer.Length; // +实体数据长
            }
            ByteBuilder builder = new ByteBuilder(capacity);
            builder.Add(BitConverter.GetBytes(capacity));
            builder.Add(BitConverter.GetBytes(cmdLength));
            builder.Add(BitConverter.GetBytes(paramLength));
            builder.Add(BitConverter.GetBytes(idLength));
            builder.Add(BitConverter.GetBytes(this.CallHashCode));
            builder.Add(Encoding.UTF8.GetBytes(this.ActionCmd));
            builder.Add(Encoding.UTF8.GetBytes(this.ActionParam));
            builder.Add(Encoding.UTF8.GetBytes(this.HttpSessionId));
            if (this.Binary != null)
            {
                builder.Add(this.Binary.Buffer);
            }
            builder.Add(BitConverter.GetBytes(capacity));
            return builder.GetBaseBuffer();
        }

        public IPEndPoint RemoteIpAddress{ get; set; }

        /// <summary>
        /// 解析数据包
        /// </summary>
        /// <param name="recvBuilder">接收到的历史数据</param>
        /// <returns></returns>
        public static DataEventArgs Parse(ByteBuilder recvBuilder)
        {

            // 数据一定要大于等于固定长度  包长小于等于数据长度
            if (recvBuilder.Count >= ConstLength && recvBuilder.GetInt32(0) <= recvBuilder.Count)
            {
                // 包长
                int totalLength = recvBuilder.ReadInt32();
                // cmdLength
                int cmdLength = recvBuilder.ReadInt32();
                int paramLength = recvBuilder.ReadInt32();
                int idLength = recvBuilder.ReadInt32();

                // 哈希值
                int hashCode = recvBuilder.ReadInt32();

                String cmd = Encoding.UTF8.GetString(recvBuilder.ReadRange(cmdLength), 0, cmdLength);
                String param = Encoding.UTF8.GetString(recvBuilder.ReadRange(paramLength), 0, paramLength);
                String id = Encoding.UTF8.GetString(recvBuilder.ReadRange(idLength), 0, idLength);

                // 实体长
                int entityLength = totalLength - ConstLength - cmdLength - paramLength - idLength;
                // 实体数据
                Binary entityBinary = new Binary(recvBuilder.ReadRange(entityLength));
                // 校验长
                int checkLength = recvBuilder.ReadInt32();

                // 检验数据
                if (totalLength == checkLength)
                {

                    // 返回数据事件包给发送者
                    return new DataEventArgs() { Binary = entityBinary, ActionCmd = cmd, CallHashCode = hashCode, ActionParam = param, HttpSessionId = id };
                }
                else
                {

                    _log.Error("无效包 清除");
                    Console.WriteLine(string.Format("{0}/{1}", totalLength, checkLength));
                    Console.WriteLine("无效包 清除");
                    // 无效包 清除
                    recvBuilder.Clear();
                    return new DataEventArgs() { Binary = null, ActionCmd = CallActionCmd.Error.ToString(), CallHashCode = hashCode, ActionParam = param, HttpSessionId = id };

                }
            }
            else
            {
                _log.Error("无效包 清除");
                Console.WriteLine(string.Format("{0}/{1}", recvBuilder.Count, recvBuilder.GetInt32(0)));
                Console.WriteLine("无效包 清除");
                recvBuilder.Clear();
                return new DataEventArgs() { Binary = null, ActionCmd = CallActionCmd.Error.ToString() };
            }
        }
    }
}
