﻿using System;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace UcAsp.WebSocket.Net
{
  /// <summary>
  /// Stores the parameters for the <see cref="SslStream"/> used by clients.
  /// </summary>
  public class ClientSslConfiguration
  {
    #region 私有字段

    private bool                                _checkCertRevocation;
    private LocalCertificateSelectionCallback   _clientCertSelectionCallback;
    private X509CertificateCollection           _clientCerts;
    private SslProtocols                        _enabledSslProtocols;
    private RemoteCertificateValidationCallback _serverCertValidationCallback;
    private string                              _targetHost;

    #endregion

    #region 构造函数

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientSslConfiguration"/> class.
    /// </summary>
    public ClientSslConfiguration ()
    {
      _enabledSslProtocols = SslProtocols.Default;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientSslConfiguration"/> class
    /// with the specified <paramref name="targetHost"/>.
    /// </summary>
    /// <param name="targetHost">
    /// A <see cref="string"/> that represents the target host server name.
    /// </param>
    public ClientSslConfiguration (string targetHost)
    {
      _targetHost = targetHost;
      _enabledSslProtocols = SslProtocols.Default;
    }

    /// <summary>
    /// Copies the parameters from the specified <paramref name="configuration"/> to
    /// a new instance of the <see cref="ClientSslConfiguration"/> class.
    /// </summary>
    /// <param name="configuration">
    /// A <see cref="ClientSslConfiguration"/> from which to copy.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="configuration"/> is <see langword="null"/>.
    /// </exception>
    public ClientSslConfiguration (ClientSslConfiguration configuration)
    {
      if (configuration == null)
        throw new ArgumentNullException ("configuration");

      _checkCertRevocation = configuration._checkCertRevocation;
      _clientCertSelectionCallback = configuration._clientCertSelectionCallback;
      _clientCerts = configuration._clientCerts;
      _enabledSslProtocols = configuration._enabledSslProtocols;
      _serverCertValidationCallback = configuration._serverCertValidationCallback;
      _targetHost = configuration._targetHost;
    }

    #endregion

    #region 属性

    /// <summary>
    /// Gets or sets a value indicating whether the certificate revocation
    /// list is checked during authentication.
    /// </summary>
    /// <value>
    ///   <para>
    ///   <c>true</c> if the certificate revocation list is checked during
    ///   authentication; otherwise, <c>false</c>.
    ///   </para>
    ///   <para>
    ///   The default value is <c>false</c>.
    ///   </para>
    /// </value>
    public bool CheckCertificateRevocation {
      get {
        return _checkCertRevocation;
      }

      set {
        _checkCertRevocation = value;
      }
    }

    /// <summary>
    /// Gets or sets the certificates from which to select one to
    /// supply to the server.
    /// </summary>
    /// <value>
    ///   <para>
    ///   A <see cref="X509CertificateCollection"/> or <see langword="null"/>.
    ///   </para>
    ///   <para>
    ///   That collection contains client certificates from which to select.
    ///   </para>
    ///   <para>
    ///   The default value is <see langword="null"/>.
    ///   </para>
    /// </value>
    public X509CertificateCollection ClientCertificates {
      get {
        return _clientCerts;
      }

      set {
        _clientCerts = value;
      }
    }

    /// <summary>
    /// Gets or sets the callback used to select the certificate to
    /// supply to the server.
    /// </summary>
    /// <remarks>
    /// No certificate is supplied if the callback returns
    /// <see langword="null"/>.
    /// </remarks>
    /// <value>
    ///   <para>
    ///   A <see cref="LocalCertificateSelectionCallback"/> delegate that
    ///   invokes the method called for selecting the certificate.
    ///   </para>
    ///   <para>
    ///   The default value is a delegate that invokes a method that
    ///   only returns <see langword="null"/>.
    ///   </para>
    /// </value>
    public LocalCertificateSelectionCallback ClientCertificateSelectionCallback {
      get {
        if (_clientCertSelectionCallback == null)
          _clientCertSelectionCallback = defaultSelectClientCertificate;

        return _clientCertSelectionCallback;
      }

      set {
        _clientCertSelectionCallback = value;
      }
    }

    /// <summary>
    /// Gets or sets the protocols used for authentication.
    /// </summary>
    /// <value>
    ///   <para>
    ///   The <see cref="SslProtocols"/> enum values that represent
    ///   the protocols used for authentication.
    ///   </para>
    ///   <para>
    ///   The default value is <see cref="SslProtocols.Default"/>.
    ///   </para>
    /// </value>
    public SslProtocols EnabledSslProtocols {
      get {
        return _enabledSslProtocols;
      }

      set {
        _enabledSslProtocols = value;
      }
    }

    /// <summary>
    /// Gets or sets the callback used to validate the certificate
    /// supplied by the server.
    /// </summary>
    /// <remarks>
    /// The certificate is valid if the callback returns <c>true</c>.
    /// </remarks>
    /// <value>
    ///   <para>
    ///   A <see cref="RemoteCertificateValidationCallback"/> delegate that
    ///   invokes the method called for validating the certificate.
    ///   </para>
    ///   <para>
    ///   The default value is a delegate that invokes a method that
    ///   only returns <c>true</c>.
    ///   </para>
    /// </value>
    public RemoteCertificateValidationCallback ServerCertificateValidationCallback {
      get {
        if (_serverCertValidationCallback == null)
          _serverCertValidationCallback = defaultValidateServerCertificate;

        return _serverCertValidationCallback;
      }

      set {
        _serverCertValidationCallback = value;
      }
    }

    /// <summary>
    /// Gets or sets the target host server name.
    /// </summary>
    /// <value>
    ///   <para>
    ///   A <see cref="string"/> or <see langword="null"/>
    ///   if not specified.
    ///   </para>
    ///   <para>
    ///   That string represents the name of the server that
    ///   will share a secure connection with a client.
    ///   </para>
    /// </value>
    public string TargetHost {
      get {
        return _targetHost;
      }

      set {
        _targetHost = value;
      }
    }

    #endregion

    #region 私有方法

    private static X509Certificate defaultSelectClientCertificate (
      object sender,
      string targetHost,
      X509CertificateCollection clientCertificates,
      X509Certificate serverCertificate,
      string[] acceptableIssuers
    )
    {
      return null;
    }

    private static bool defaultValidateServerCertificate (
      object sender,
      X509Certificate certificate,
      X509Chain chain,
      SslPolicyErrors sslPolicyErrors
    )
    {
      return true;
    }

    #endregion
  }
}
