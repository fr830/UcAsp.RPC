using System;

namespace UcAsp.WebSocket.Net
{
  internal class Chunk
  {
    #region ˽���ֶ�

    private byte[] _data;
    private int    _offset;

    #endregion

    #region ���캯��

    public Chunk (byte[] data)
    {
      _data = data;
    }

    #endregion

    #region ����

    public int ReadLeft {
      get {
        return _data.Length - _offset;
      }
    }

    #endregion

    #region ����

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
