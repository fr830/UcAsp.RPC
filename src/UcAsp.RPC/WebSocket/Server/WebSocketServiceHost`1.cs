using System;

namespace UcAsp.WebSocket.Server
{
    internal class WebSocketServiceHost<TBehavior> : WebSocketServiceHost
      where TBehavior : WebSocketBehavior
    {
        #region ˽���ֶ�

        private Func<TBehavior> _creator;

        #endregion

        #region �ڲ����캯��

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

        #region ����

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

        #region ˽�з���

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
