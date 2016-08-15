/***************************************************
*创建人:TecD02
*创建时间:2016/7/30 18:51:42
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
{ /// <summary>
  /// 可变长byte集合
  /// </summary>
    public class ByteBuilder
    {
        /// <summary>
        /// 原始数据
        /// </summary>
        private byte[] _baseBuffer = null;

        /// <summary>
        /// 默认容量
        /// </summary>
        private int _defaultCapacity = 0;

        /// <summary>
        /// 容量
        /// </summary>
        public int Capacity { get; private set; }

        public object SyncLock { get; private set; }

        /// <summary>
        /// 有效数据长度
        /// </summary>
        public int Count { get; private set; }



        public ByteBuilder(int capacity)
        {
            this.Capacity = capacity;
            this._defaultCapacity = capacity;
            this._baseBuffer = new byte[capacity];
            this.SyncLock = new object();
        }

        /// <summary>
        /// 数据添加到集合
        /// </summary>
        /// <param name="srcArray">数据</param>
        public void Add(byte[] srcArray)
        {
            lock (this.SyncLock)
            {
                this.Add(srcArray, 0, srcArray.Length);
            }
        }

        /// <summary>
        /// 数据添加到集合
        /// </summary>
        /// <param name="srcArray">数据</param>
        /// <param name="index">起始位置</param>
        /// <param name="count">复制长度</param>
        public void Add(byte[] srcArray, int index, int count)
        {
            lock (this.SyncLock)
            {
                if (srcArray == null)
                {
                    return;
                }

                int newLength = this.Count + count;
                if (newLength > this.Capacity)
                {
                    while (newLength > this.Capacity)
                    {
                        this.Capacity = this.Capacity * 2;
                    }

                    byte[] newBuffer = new byte[this.Capacity];
                    this._baseBuffer.CopyTo(newBuffer, 0);
                    this._baseBuffer = newBuffer;
                }

                try
                {
                    Array.Copy(srcArray, index, this._baseBuffer, this.Count, count);
                }
                catch (Exception ex)
                { }
                this.Count = newLength;
            }
        }

        /// <summary>
        /// 从0位置将数据剪切到指定数组
        /// </summary>
        /// <param name="destArray">目标数组</param>
        /// <param name="index">目标数据索引</param>
        /// <param name="count">剪切长度</param>
        public void CutTo(byte[] destArray, int index, int count)
        {
            lock (this.SyncLock)
            {
                this.CopyTo(destArray, index, count);
                this.RemoveRange(count);
            }
        }

        /// <summary>
        /// 从0位置清除指定长度的字节
        /// </summary>
        /// <param name="count"></param>
        public void RemoveRange(int count)
        {
            lock (this.SyncLock)
            {
                this.Count = this.Count - count;
                Array.Copy(this._baseBuffer, count, this._baseBuffer, 0, this.Count);
            }
        }

        /// <summary>
        /// 从0位置将数据复制到指定数组
        /// </summary>
        /// <param name="destArray">目标数组</param>
        /// <param name="index">目标数据索引</param>
        /// <param name="count">复制长度</param>
        public void CopyTo(byte[] destArray, int index, int count)
        {
            lock (this.SyncLock)
            {
                Array.Copy(this._baseBuffer, 0, destArray, index, count);
            }
        }

        /// <summary>
        /// 返回有效数据的数组
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray()
        {
            lock (this.SyncLock)
            {
                byte[] buffer = new byte[this.Count];
                Array.Copy(this._baseBuffer, 0, buffer, 0, this.Count);
                return buffer;
            }
        }

        /// <summary>
        /// 获取基础数据        
        /// </summary>
        /// <returns></returns>
        public byte[] GetBaseBuffer()
        {
            return this._baseBuffer;
        }

        /// <summary>
        /// 返回有效二进制数据
        /// </summary>
        /// <returns></returns>
        public Binary ToBinary()
        {
            return new Binary(this.ToArray());
        }

        /// <summary>
        /// 读取指定位置一个字节
        /// </summary>
        /// <param name="index">字节所在索引</param>
        /// <returns></returns>
        public byte GetByte(int index)
        {
            lock (this.SyncLock)
            {
                return this._baseBuffer[index];
            }
        }

        /// <summary>
        /// 读取指定位置4个字节，返回其Int32表示类型
        /// </summary>
        /// <param name="index">字节所在索引</param>
        /// <returns></returns>
        public int GetInt32(int index)
        {
            lock (this.SyncLock)
            {
                return BitConverter.ToInt32(this._baseBuffer, index);
            }
        }

        /// <summary>
        /// 从0位置读取并删除一个字节
        /// </summary>      
        /// <returns></returns>
        public byte ReadByte()
        {
            lock (this.SyncLock)
            {
                byte b = this.GetByte(0);
                this.RemoveRange(1);
                return b;
            }
        }

        /// <summary>
        /// 从0位置读取并删除4个字节，返回其Int32表示类型     
        /// </summary> 
        /// <returns></returns>
        public int ReadInt32()
        {
            lock (this.SyncLock)
            {
                int int32 = this.GetInt32(0);
                this.RemoveRange(4);
                return int32;
            }
        }


        /// <summary>
        /// 从0位置读取并清除指定长度的字节
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public byte[] ReadRange(int count)
        {
            lock (this.SyncLock)
            {
                byte[] buffer = new byte[count];
                this.CutTo(buffer, 0, count);
                return buffer;
            }
        }

        /// <summary>
        /// 清空数据 
        /// </summary>
        /// <returns></returns>
        public void Clear()
        {
            lock (this.SyncLock)
            {
                this.Count = 0;
                this._baseBuffer = new byte[this.Capacity];
            }
        }

        /// <summary>
        /// 重置为初始状态
        /// </summary>
        public void ReSet()
        {
            lock (this.SyncLock)
            {
                this.Count = 0;
                this.Capacity = this._defaultCapacity;
                this._baseBuffer = new byte[this.Capacity];
            }
        }
    }
}
