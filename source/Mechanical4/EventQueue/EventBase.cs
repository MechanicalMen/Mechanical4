using System;
using System.Reflection;
using System.Threading;

namespace Mechanical4.EventQueue
{
    /// <summary>
    /// The base class all events must inherit.
    /// </summary>
    public abstract class EventBase
    {
        private string pos;

        /// <summary>
        /// Gets the position in the source file, from where this event was enqueued the last time;
        /// or <c>null</c>, if the event was never enqueued.
        /// The value is used by the queue, for throwing more informative exceptions
        /// (since the event source is not part of the stack tree of an event handler's exception).
        /// </summary>
        public string EventEnqueuePos
        {
            get => Interlocked.CompareExchange(ref this.pos, null, null);
            internal set => Interlocked.Exchange(ref this.pos, value);
        }

        #region Public Static Methods

        /// <summary>
        /// Determines whether the specified event handler, can handle the specified event.
        /// </summary>
        /// <typeparam name="TEventHandlerEvent">The type of events, the event handler expects.</typeparam>
        /// <typeparam name="TEvent">The type of event to check.</typeparam>
        /// <param name="eventHandler">An event handler.</param>
        /// <param name="evnt">The event to check.</param>
        /// <returns><c>true</c> if the <paramref name="eventHandler"/> can handle the specified <paramref name="evnt"/>; otherwise, <c>false</c>.</returns>
        public static bool CanHandle<TEventHandlerEvent, TEvent>( IEventHandler<TEventHandlerEvent> eventHandler, TEvent evnt )
            where TEventHandlerEvent : EventBase
            where TEvent : EventBase
        {
            return CanHandle<TEventHandlerEvent, TEvent>();
        }

        /// <summary>
        /// Determines whether an <see cref="IEventHandler{T}"/>, handling events of type <typeparamref name="TEventHandlerEvent"/>
        /// can handle events of type <typeparamref name="TEvent"/>.
        /// </summary>
        /// <returns><c>true</c> if an <see cref="IEventHandler{T}"/>, handling events of type <typeparamref name="TEventHandlerEvent"/>,
        /// can handle events of type <typeparamref name="TEvent"/>; otherwise, <c>false</c>.</returns>
        public static bool CanHandle<TEventHandlerEvent, TEvent>()
            where TEventHandlerEvent : EventBase
            where TEvent : EventBase
        {
            return CanHandle(typeof(TEventHandlerEvent).GetTypeInfo(), typeof(TEvent).GetTypeInfo());
        }

        /// <summary>
        /// Determines whether an <see cref="IEventHandler{T}"/>, handling events of type <paramref name="eventHandlerEventType"/>,
        /// can handle events of type <paramref name="eventType"/>.
        /// </summary>
        /// <param name="eventHandlerEventType">The type of events handled by an <see cref="IEventHandler{T}"/>.</param>
        /// <param name="eventType">The type of event to check.</param>
        /// <returns><c>true</c> if an <see cref="IEventHandler{T}"/>, handling events of type <paramref name="eventHandlerEventType"/>,
        /// can handle events of type <paramref name="eventType"/>; otherwise, <c>false</c>.</returns>
        public static bool CanHandle( Type eventHandlerEventType, Type eventType )
        {
            return CanHandle(eventHandlerEventType?.GetTypeInfo(), eventType?.GetTypeInfo());
        }

        /// <summary>
        /// Determines whether an <see cref="IEventHandler{T}"/>, handling events of type <paramref name="eventHandlerEventTypeInfo"/>,
        /// can handle events of type <paramref name="eventTypeInfo"/>.
        /// </summary>
        /// <param name="eventHandlerEventTypeInfo">The type of events handled by an <see cref="IEventHandler{T}"/>.</param>
        /// <param name="eventTypeInfo">The type of event to check.</param>
        /// <returns><c>true</c> if an <see cref="IEventHandler{T}"/>, handling events of type <paramref name="eventHandlerEventTypeInfo"/>,
        /// can handle events of type <paramref name="eventTypeInfo"/>; otherwise, <c>false</c>.</returns>
        public static bool CanHandle( TypeInfo eventHandlerEventTypeInfo, TypeInfo eventTypeInfo )
        {
            if( eventHandlerEventTypeInfo.NullReference() )
                throw Exc.Null(nameof(eventHandlerEventTypeInfo));

            if( eventTypeInfo.NullReference() )
                throw Exc.Null(nameof(eventTypeInfo));

            return eventHandlerEventTypeInfo.IsAssignableFrom(eventTypeInfo);
        }

        #endregion
    }
}
