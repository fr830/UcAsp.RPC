using System;

namespace UcAsp.WebSocket.Server
{
    internal class WebSocketServiceHost<TBehavior> : WebSocketServiceHost
      where TBehavior : WebSocketBehavior
    {
        #region 私有字段

        private Func<TBehavior> _creator;

        #endregion

        #region 内部构造函数

        internal WebSocketServiceHost(
          string path, Func<TBehavior> creator, Logger log
        )
          : this(path, creator, null, log)
        {
        }

        internal WebSocketServiceHost(
          string path,
          Func<TBehavior> creator,
          Action<TBehavior> initializer,
          Logger log
        )
          : base(path, log)
        {
            _creator = createCreator(creator, initializer);
        }

        #endregion

        #region 属性

        public override Type BehaviorType
        {
            get
            {
                return typeof(TBehavior);
            }
        }
        public override WebSocketBehavior Behavior
        {
            get
            {
                return CreateSession();
            }
        }
        #endregion

        #region 私有方法

        private Func<TBehavior> createCreator(
      Func<TBehavior> creator, Action<TBehavior> initializer
    )
        {
            if (initializer == null)
                return creator;

            return () =>
            {
                var ret = creator();
                initializer(ret);

                return ret;
            };
        }

        #endregion

        #region Protected Methods

        protected override WebSocketBehavior CreateSession()
        {
            return _creator();
        }

        #endregion
    }
}
