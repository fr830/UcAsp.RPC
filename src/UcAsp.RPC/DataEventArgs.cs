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

        public StatusCode StatusCode { get; set; }
        public String ActionCmd { get; set; }

        public String ActionParam { get; set; }

        public String HttpSessionId { get; set; }

        public int TaskId { get; set; }

        public string LastError { get; set; }
        public string RemoteIpAddress { get; set; }
        public int TryTimes { get; set; }
        // 无数据时包的固定长度 24
        //总包长4 + ActionCmd长 4 + ActionParam长 4 + SessionId长 4 + hashCode 4 + 验校总包长 4
        //数据格式   [包长 4byte][ActionCmd长 4byte][ActionParam长 4byte][SessionId长 4byte][hashCode 4byte][ActionCmd N个byte][ActionParam N个byte][SessionId N个byte][实体 N个byte][包长 4byte]
        private const int ConstLength = 44;


        public DataEventArgs()
        {
            this.CallHashCode = 0;
            this.ActionCmd = "";
            this.ActionParam = "";
            this.HttpSessionId = "";

            this.TryTimes = 0;
            this.StatusCode = StatusCode.Normal;
            this.TaskId = 0;
            this.LastError = "";
            this.RemoteIpAddress = null;

        }

        /// <summary>
        /// 转换为二进制数据
        /// </summary>
        /// <returns></returns>
        public byte[] ToByteArray()
        {
            int cmdLength = this.ActionCmd.Length;
            int paramLength = Encoding.UTF8.GetByteCount(this.ActionParam);
            int idLength = Encoding.UTF8.GetByteCount(HttpSessionId);
            int errLength = Encoding.UTF8.GetByteCount(LastError);
            int ipLength = 0;
            if (!string.IsNullOrEmpty(this.RemoteIpAddress))
            {
                ipLength = Encoding.UTF8.GetByteCount(RemoteIpAddress);
            }
            int capacity = ConstLength + cmdLength + paramLength + idLength + errLength + ipLength;

            if (this.Binary != null && this.Binary.Buffer != null)
            {
                capacity = capacity + this.Binary.Buffer.Length; // +实体数据长
            }
            ByteBuilder builder = new ByteBuilder(capacity);
            builder.Add(BitConverter.GetBytes(capacity));
            builder.Add(BitConverter.GetBytes(cmdLength));
            builder.Add(BitConverter.GetBytes(paramLength));
            builder.Add(BitConverter.GetBytes(idLength));
            builder.Add(BitConverter.GetBytes(errLength));
            builder.Add(BitConverter.GetBytes(this.CallHashCode));


            builder.Add(BitConverter.GetBytes(this.TaskId));
            builder.Add(BitConverter.GetBytes((int)this.StatusCode));
            builder.Add(BitConverter.GetBytes(this.TryTimes));
            builder.Add(BitConverter.GetBytes(ipLength));



            builder.Add(Encoding.UTF8.GetBytes(this.ActionCmd));

            builder.Add(Encoding.UTF8.GetBytes(this.ActionParam));

            builder.Add(Encoding.UTF8.GetBytes(this.HttpSessionId));

            if (!string.IsNullOrEmpty(RemoteIpAddress))
            {
                builder.Add(Encoding.UTF8.GetBytes(RemoteIpAddress));
            }

            if (!string.IsNullOrEmpty(LastError))
            {
                builder.Add(Encoding.UTF8.GetBytes(this.LastError));
            }
            if (this.Binary != null)
            {
                builder.Add(this.Binary.Buffer);
            }
            builder.Add(BitConverter.GetBytes(capacity));
            return builder.GetBaseBuffer();
        }



        /// <summary>
        /// 解析数据包
        /// </summary>
        /// <param name="recvBuilder">接收到的历史数据</param>
        /// <returns></returns>
        public static DataEventArgs Parse(ByteBuilder recvBuilder)
        {
            int count = recvBuilder.Count;
            int total = recvBuilder.GetInt32(0);
            // 数据一定要大于等于固定长度  包长小于等于数据长度
            if (recvBuilder.Count >= ConstLength && recvBuilder.GetInt32(0) <= recvBuilder.Count)
            {
                // 包长
                int totalLength = recvBuilder.ReadInt32();
                // cmdLength
                int cmdLength = recvBuilder.ReadInt32();
                int paramLength = recvBuilder.ReadInt32();
                int idLength = recvBuilder.ReadInt32();

                int errLength = recvBuilder.ReadInt32();
                // 哈希值
                int hashCode = recvBuilder.ReadInt32();
                int taskId = recvBuilder.ReadInt32();
                int statusCode = recvBuilder.ReadInt32();
                int tryTimes = recvBuilder.ReadInt32();
                int ipLength = recvBuilder.ReadInt32();
                String cmd = Encoding.UTF8.GetString(recvBuilder.ReadRange(cmdLength), 0, cmdLength);
                String param = Encoding.UTF8.GetString(recvBuilder.ReadRange(paramLength), 0, paramLength);
                String id = Encoding.UTF8.GetString(recvBuilder.ReadRange(idLength), 0, idLength);
                byte[] errBinary = new Binary(recvBuilder.ReadRange(errLength)).Buffer;
                byte[] ipBinary = new Binary(recvBuilder.ReadRange(ipLength)).Buffer;
                string ipAddress = string.Empty;
                if (ipBinary.Length > 0)
                {
                    ipAddress = Encoding.UTF8.GetString(ipBinary);
                }
                string lastError = string.Empty;
                if (errBinary.Length > 0)
                {
                    lastError = Encoding.UTF8.GetString(errBinary);
                }
                // 实体长
                int entityLength = totalLength - ConstLength - cmdLength - paramLength - idLength - errLength - ipLength;
                // 实体数据
                Binary entityBinary = new Binary(recvBuilder.ReadRange(entityLength));
                // 校验长
                int checkLength = recvBuilder.ReadInt32();

                // 检验数据
                if (totalLength == checkLength)
                {

                    // 返回数据事件包给发送者
                    return new DataEventArgs() { Binary = entityBinary, LastError = lastError, RemoteIpAddress = ipAddress, TryTimes = tryTimes, StatusCode = (StatusCode)statusCode, TaskId = taskId, ActionCmd = cmd, CallHashCode = hashCode, ActionParam = param, HttpSessionId = id };
                }
                else
                {

                    _log.Error("无效包 包转换失败 清除");
                    // 无效包 清除
                    recvBuilder.Clear();
                    return new DataEventArgs() { Binary = null, LastError = "包转换失败", ActionCmd = CallActionCmd.Call.ToString(), StatusCode = StatusCode.Error, TaskId = 9999, CallHashCode = hashCode, ActionParam = param, HttpSessionId = id };

                }
            }
            else
            {
                _log.Error("空包 网络丢失 清除");
                recvBuilder.Clear();
                return new DataEventArgs() { Binary = null, LastError = "网络丢失", StatusCode = StatusCode.Error, ActionCmd = CallActionCmd.Call.ToString() };

            }
        }
    }
}
