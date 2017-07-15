
using System;

namespace UcAsp.WebSocket.Net
{
  internal class HttpHeaderInfo
  {
    #region 私有字段

    private string         _name;
    private HttpHeaderType _type;

    #endregion

    #region 内部构造函数

    internal HttpHeaderInfo (string name, HttpHeaderType type)
    {
      _name = name;
      _type = type;
    }

    #endregion

    #region 内部属性

    internal bool IsMultiValueInRequest {
      get {
        return (_type & HttpHeaderType.MultiValueInRequest) == HttpHeaderType.MultiValueInRequest;
      }
    }

    internal bool IsMultiValueInResponse {
      get {
        return (_type & HttpHeaderType.MultiValueInResponse) == HttpHeaderType.MultiValueInResponse;
      }
    }

    #endregion

    #region 属性

    public bool IsRequest {
      get {
        return (_type & HttpHeaderType.Request) == HttpHeaderType.Request;
      }
    }

    public bool IsResponse {
      get {
        return (_type & HttpHeaderType.Response) == HttpHeaderType.Response;
      }
    }

    public string Name {
      get {
        return _name;
      }
    }

    public HttpHeaderType Type {
      get {
        return _type;
      }
    }

    #endregion

    #region 方法

    public bool IsMultiValue (bool response)
    {
      return (_type & HttpHeaderType.MultiValue) == HttpHeaderType.MultiValue
             ? (response ? IsResponse : IsRequest)
             : (response ? IsMultiValueInResponse : IsMultiValueInRequest);
    }

    public bool IsRestricted (bool response)
    {
      return (_type & HttpHeaderType.Restricted) == HttpHeaderType.Restricted
             ? (response ? IsResponse : IsRequest)
             : false;
    }

    #endregion
  }
}
