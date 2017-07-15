using System;
using System.Collections;
using System.Collections.Generic;

namespace UcAsp.WebSocket
{
  internal class PayloadData : IEnumerable<byte>
  {
    #region 私有字段

    private ushort _code;
    private bool   _codeSet;
    private byte[] _data;
    private long   _extDataLength;
    private long   _length;
    private string _reason;
    private bool   _reasonSet;

    #endregion

    #region Public Fields

    /// <summary>
    /// Represents the empty payload data.
    /// </summary>
    public static readonly PayloadData Empty;

    /// <summary>
    /// Represents the allowable max length.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   A <see cref="WebSocketException"/> will occur if the payload data length is
    ///   greater than the value of this field.
    ///   </para>
    ///   <para>
    ///   If you would like to change the value, you must set it to a value between
    ///   <c>WebSocket.FragmentLength</c> and <c>Int64.MaxValue</c> inclusive.
    ///   </para>
    /// </remarks>
    public static readonly ulong MaxLength;

    #endregion

    #region 静态构造函数

    static PayloadData ()
    {
      Empty = new PayloadData ();
      MaxLength = Int64.MaxValue;
    }

    #endregion

    #region 内部构造函数

    internal PayloadData ()
    {
      _code = 1005;
      _reason = String.Empty;

      _data = WebSocket.EmptyBytes;

      _codeSet = true;
      _reasonSet = true;
    }

    internal PayloadData (byte[] data)
      : this (data, data.LongLength)
    {
    }

    internal PayloadData (byte[] data, long length)
    {
      _data = data;
      _length = length;
    }

    internal PayloadData (ushort code, string reason)
    {
      _code = code;
      _reason = reason ?? String.Empty;

      _data = code.Append (reason);
      _length = _data.LongLength;

      _codeSet = true;
      _reasonSet = true;
    }

    #endregion

    #region 内部属性

    internal ushort Code {
      get {
        if (!_codeSet) {
          _code = _length > 1
                  ? _data.SubArray (0, 2).ToUInt16 (ByteOrder.Big)
                  : (ushort) 1005;

          _codeSet = true;
        }

        return _code;
      }
    }

    internal long ExtensionDataLength {
      get {
        return _extDataLength;
      }

      set {
        _extDataLength = value;
      }
    }

    internal bool HasReservedCode {
      get {
        return _length > 1 && Code.IsReserved ();
      }
    }

    internal string Reason {
      get {
        if (!_reasonSet) {
          _reason = _length > 2
                    ? _data.SubArray (2, _length - 2).UTF8Decode ()
                    : String.Empty;

          _reasonSet = true;
        }

        return _reason;
      }
    }

    #endregion

    #region 属性

    public byte[] ApplicationData {
      get {
        return _extDataLength > 0
               ? _data.SubArray (_extDataLength, _length - _extDataLength)
               : _data;
      }
    }

    public byte[] ExtensionData {
      get {
        return _extDataLength > 0
               ? _data.SubArray (0, _extDataLength)
               : WebSocket.EmptyBytes;
      }
    }

    public ulong Length {
      get {
        return (ulong) _length;
      }
    }

    #endregion

    #region 内部方法

    internal void Mask (byte[] key)
    {
      for (long i = 0; i < _length; i++)
        _data[i] = (byte) (_data[i] ^ key[i % 4]);
    }

    #endregion

    #region 方法

    public IEnumerator<byte> GetEnumerator ()
    {
      foreach (var b in _data)
        yield return b;
    }

    public byte[] ToArray ()
    {
      return _data;
    }

    public override string ToString ()
    {
      return BitConverter.ToString (_data);
    }

    #endregion

    #region Explicit Interface Implementations

    IEnumerator IEnumerable.GetEnumerator ()
    {
      return GetEnumerator ();
    }

    #endregion
  }
}
