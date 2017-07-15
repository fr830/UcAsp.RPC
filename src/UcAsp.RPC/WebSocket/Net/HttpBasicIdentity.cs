
using System;
using System.Security.Principal;

namespace UcAsp.WebSocket.Net
{
  /// <summary>
  /// Holds the username and password from an HTTP Basic authentication attempt.
  /// </summary>
  public class HttpBasicIdentity : GenericIdentity
  {
    #region 私有字段

    private string _password;

    #endregion

    #region 内部构造函数

    internal HttpBasicIdentity (string username, string password)
      : base (username, "Basic")
    {
      _password = password;
    }

    #endregion

    #region 属性

    /// <summary>
    /// Gets the password from a basic authentication attempt.
    /// </summary>
    /// <value>
    /// A <see cref="string"/> that represents the password.
    /// </value>
    public virtual string Password {
      get {
        return _password;
      }
    }

    #endregion
  }
}
