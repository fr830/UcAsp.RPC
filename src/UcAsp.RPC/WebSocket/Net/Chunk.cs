using System;

namespace UcAsp.WebSocket.Net
{
  internal class Chunk
  {
    #region 私有字段

    private byte[] _data;
    private int    _offset;

    #endregion

    #region 构造函数

    public Chunk (byte[] data)
    {
      _data = data;
    }

    #endregion

    #region 属性

    public int ReadLeft {
      get {
        return _data.Length - _offset;
      }
    }

    #endregion

    #region 方法

    public int Read (byte[] buffer, int offset, int count)
    {
      var left = _data.Length - _offset;
      if (left == 0)
        return left;

      if (count > left)
        count = left;

      Buffer.BlockCopy (_data, _offset, buffer, offset, count);
      _offset += count;

      return count;
    }

    #endregion
  }
}
