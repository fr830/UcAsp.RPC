
using System;

namespace UcAsp.WebSocket.Net
{
  internal class HttpHeaderInfo
  {
    #region ˽���ֶ�

    private string         _name;
    private HttpHeaderType _type;

    #endregion

    #region �ڲ����캯��

    internal HttpHeaderInfo (string name, HttpHeaderType type)
    {
      _name = name;
      _type = type;
    }

    #endregion

    #region �ڲ�����

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

    #region ����

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

    #region ����

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
