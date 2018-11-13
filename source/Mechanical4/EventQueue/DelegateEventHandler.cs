using System;

namespace Mechanical4.EventQueue
{
    /// <summary>
    /// Encapsulates delegates as event handlers.
    /// </summary>
    public static class DelegateEventHandler
    {
        #region FuncEventHandler

        private class FuncEventHandler<TEvent, TState> : IEventHandler<TEvent>
            where TEvent : EventBase
        {
            private readonly object syncLock = new object();
            private readonly Func<TState, TEvent, TState> dlgt;
            private TState currentState;

            internal FuncEventHandler( Func<TState, TEvent, TState> func, TState initialState )
            {
                this.dlgt = func ?? throw Exc.Null(nameof(func));
                this.currentState = initialState;
            }

            public void Handle( TEvent evnt )
            {
                lock( this.syncLock ) // necessary, in case this handler is subscribed to multiple event queues
                    this.currentState = this.dlgt(this.currentState, evnt); // no state change if an exception is thrown!
            }
        }

        #endregion

        /// <summary>
        /// Creates a new event handler, with an internal state.
        /// </summary>
        /// <typeparam name="TEvent">The type of events to handle instances of. The actual type of those instances may be a subclass of <typeparamref name="TEvent"/>.</typeparam>
        /// <typeparam name="TState">The type of the internal state.</typeparam>
        /// <param name="handler">The event handler delegate to use.</param>
        /// <param name="initialState">The initial internal state.</param>
        /// <returns>A new event handler instance.</returns>
        public static IEventHandler<TEvent> On<TEvent, TState>( Func<TState, TEvent, TState> handler, TState initialState = default )
            where TEvent : EventBase
        {
            return new FuncEventHandler<TEvent, TState>(handler, initialState);
        }

        /// <summary>
        /// Creates a new, stateless event handler.
        /// </summary>
        /// <typeparam name="TEvent">The type of events to handle instances of. The actual type of those instances may be a subclass of <typeparamref name="TEvent"/>.</typeparam>
        /// <param name="handler">The event handler delegate to use.</param>
        /// <returns>A new event handler instance.</returns>
        public static IEventHandler<TEvent> On<TEvent>( Action<TEvent> handler )
            where TEvent : EventBase
        {
            if( handler.NullReference() )
                throw Exc.Null(nameof(handler));

            return On<TEvent, object>(
                ( currentState, evnt ) =>
                {
                    handler(evnt);
                    return null;
                },
                initialState: null);
        }

        /// <summary>
        /// Creates a new, stateless event handler for <see cref="UnhandledExceptionEvent"/>.
        /// </summary>
        /// <param name="handler">The event handler delegate to use.</param>
        /// <returns>A new event handler instance.</returns>
        public static IEventHandler<UnhandledExceptionEvent> OnException( Action<UnhandledExceptionEvent> handler )
        {
            return On(handler);
        }

        /// <summary>
        /// Creates a new, stateless event handler for <see cref="ShutdownRequestEvent"/>.
        /// </summary>
        /// <param name="handler">The event handler delegate to use.</param>
        /// <returns>A new event handler instance.</returns>
        public static IEventHandler<ShutdownRequestEvent> OnShutdownRequest( Action<ShutdownRequestEvent> handler )
        {
            return On(handler);
        }

        /// <summary>
        /// Creates a new, stateless event handler for <see cref="ShuttingDownEvent"/>.
        /// </summary>
        /// <param name="handler">The event handler delegate to use.</param>
        /// <returns>A new event handler instance.</returns>
        public static IEventHandler<ShuttingDownEvent> OnShuttingDown( Action<ShuttingDownEvent> handler )
        {
            return On(handler);
        }
    }
}
