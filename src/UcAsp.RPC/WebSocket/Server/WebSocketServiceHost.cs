
using System;
using UcAsp.WebSocket.Net.WebSockets;
using UcAsp.WebSocket.Net;
namespace UcAsp.WebSocket.Server
{
    /// <summary>
    /// Exposes the methods and properties used to access the information in
    /// a WebSocket service provided by the <see cref="WebSocketServer"/> or
    /// <see cref="HttpServer"/>.
    /// </summary>
    /// <remarks>
    /// This class is an abstract class.
    /// </remarks>
    public abstract class WebSocketServiceHost
    {
        #region 私有字段

        private Logger _log;
        private string _path;
        private WebSocketSessionManager _sessions;

        #endregion

        #region Protected Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketServiceHost"/> class
        /// with the specified <paramref name="path"/> and <paramref name="log"/>.
        /// </summary>
        /// <param name="path">
        /// A <see cref="string"/> that represents the absolute path to the service.
        /// </param>
        /// <param name="log">
        /// A <see cref="Logger"/> that represents the logging function for the service.
        /// </param>
        protected WebSocketServiceHost(string path, Logger log)
        {
            _path = path;
            _log = log;

            _sessions = new WebSocketSessionManager(log);
        }

        #endregion

        #region 内部属性

        internal ServerState State
        {
            get
            {
                return _sessions.State;
            }
        }

        #endregion

        #region Protected Properties

        /// <summary>
        /// Gets the logging function for the service.
        /// </summary>
        /// <value>
        /// A <see cref="Logger"/> that provides the logging function.
        /// </value>
        protected Logger Log
        {
            get
            {
                return _log;
            }
        }

        #endregion

        #region 属性

        /// <summary>
        /// Gets or sets a value indicating whether the service cleans up
        /// the inactive sessions periodically.
        /// </summary>
        /// <remarks>
        /// The set operation does nothing if the service has already started or
        /// it is shutting down.
        /// </remarks>
        /// <value>
        /// <c>true</c> if the service cleans up the inactive sessions every 60
        /// seconds; otherwise, <c>false</c>.
        /// </value>
        public bool KeepClean
        {
            get
            {
                return _sessions.KeepClean;
            }

            set
            {
                string msg;
                if (!canSet(out msg))
                {
                    _log.Warn(msg);
                    return;
                }

                _sessions.KeepClean = value;
            }
        }

        /// <summary>
        /// Gets the path to the service.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the absolute path to
        /// the service.
        /// </value>
        public string Path
        {
            get
            {
                return _path;
            }
        }

        /// <summary>
        /// Gets the management function for the sessions in the service.
        /// </summary>
        /// <value>
        /// A <see cref="WebSocketSessionManager"/> that manages the sessions in
        /// the service.
        /// </value>
        public WebSocketSessionManager Sessions
        {
            get
            {
                return _sessions;
            }
        }

        /// <summary>
        /// Gets the <see cref="Type"/> of the behavior of the service.
        /// </summary>
        /// <value>
        /// A <see cref="Type"/> that represents the type of the behavior of
        /// the service.
        /// </value>
        public abstract Type BehaviorType { get; }
        public abstract WebSocketBehavior Behavior { get; }
        /// <summary>
        /// Gets or sets the wait time for the response to the WebSocket Ping or
        /// Close.
        /// </summary>
        /// <remarks>
        /// The set operation does nothing if the service has already started or
        /// it is shutting down.
        /// </remarks>
        /// <value>
        /// A <see cref="TimeSpan"/> that represents the wait time for
        /// the response.
        /// </value>
        /// <exception cref="ArgumentException">
        /// The value specified for a set operation is zero or less.
        /// </exception>
        public TimeSpan WaitTime
        {
            get
            {
                return _sessions.WaitTime;
            }

            set
            {
                string msg;
                if (!value.CheckWaitTime(out msg))
                    throw new ArgumentException(msg, "value");

                if (!canSet(out msg))
                {
                    _log.Warn(msg);
                    return;
                }

                _sessions.WaitTime = value;
            }
        }

        #endregion

        #region 私有方法

        private bool canSet(out string message)
        {
            message = null;

            var state = _sessions.State;
            if (state == ServerState.Start)
            {
                message = "The service has already started.";
                return false;
            }

            if (state == ServerState.ShuttingDown)
            {
                message = "The service is shutting down.";
                return false;
            }

            return true;
        }

        #endregion

        #region 内部方法

        internal void Start()
        {
            _sessions.Start();
        }
        internal void StartSession(HttpListenerContext context)
        {
            CreateSession().Start(context, _sessions);
        }
        internal void StartSession(WebSocketContext context)
        {
            CreateSession().Start(context, _sessions);
        }

        internal void Stop(ushort code, string reason)
        {
            _sessions.Stop(code, reason);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Creates a new session for the service.
        /// </summary>
        /// <returns>
        /// A <see cref="WebSocketBehavior"/> instance that represents
        /// the new session.
        /// </returns>
        protected abstract WebSocketBehavior CreateSession();

        #endregion
    }
}
