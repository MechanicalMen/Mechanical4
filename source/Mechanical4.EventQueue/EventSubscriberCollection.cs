using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Mechanical4.Core;

namespace Mechanical4.EventQueue
{
    /// <summary>
    /// A thread-safe collection of event subscribers.
    /// Once the event queue is closed, the collection is disabled,
    /// and method calls will have no effect.
    /// </summary>
    public class EventSubscriberCollection
    {
        #region Subscriber wrappers

        private delegate ISubscriberWrapper WrapperConstructor( object eventHandlerInstance, EventHandlerInfo eventHandlerInfo );

        private interface ISubscriberWrapper
        {
            EventHandlerInfo EventHandlerInfo { get; }

            object AcquireStrongRef();
            void Handle( EventBase evnt );
            void ReleaseStrongRef();
        }

        private class StrongSubscriber<T> : ISubscriberWrapper
            where T : EventBase
        {
            private readonly IEventHandler<T> eventHandlerInstance;

            internal StrongSubscriber( IEventHandler<T> handler, EventHandlerInfo info )
            {
                this.eventHandlerInstance = handler ?? throw Exc.Null(nameof(handler));
                this.EventHandlerInfo = info ?? throw Exc.Null(nameof(info));
            }

            public EventHandlerInfo EventHandlerInfo { get; }

            public object AcquireStrongRef() => this.eventHandlerInstance;
            public void Handle( EventBase evnt ) => this.eventHandlerInstance.Handle((T)evnt);
            public void ReleaseStrongRef() { }
        }

        private class WeakSubscriber<T> : ISubscriberWrapper
            where T : EventBase
        {
            private readonly WeakReference<IEventHandler<T>> weakRef;
            private IEventHandler<T> temporaryStrongRef = null;

            internal WeakSubscriber( IEventHandler<T> handler, EventHandlerInfo info )
            {
                if( handler.NullReference() )
                    throw Exc.Null(nameof(handler));

                this.weakRef = new WeakReference<IEventHandler<T>>(handler);
                this.EventHandlerInfo = info ?? throw Exc.Null(nameof(info));
            }

            public EventHandlerInfo EventHandlerInfo { get; }

            public object AcquireStrongRef() => this.weakRef.TryGetTarget(out this.temporaryStrongRef) ? this.temporaryStrongRef : null;
            public void Handle( EventBase evnt ) => this.temporaryStrongRef.Handle((T)evnt);
            public void ReleaseStrongRef() => this.temporaryStrongRef = null;
        }

        #endregion

        #region EventHandlerInfo

        private class EventHandlerInfo
        {
            internal static EventHandlerInfo From( Type interfaceType )
            {
                // if this interface an IEventHandler<?>
                var interfaceTypeInfo = interfaceType.GetTypeInfo();
                if( interfaceTypeInfo.IsGenericType
                 && interfaceTypeInfo.GetGenericTypeDefinition() == typeof(IEventHandler<>) )
                {
                    var eventType = interfaceTypeInfo.GenericTypeArguments[0];
                    return new EventHandlerInfo(eventType, interfaceType, interfaceTypeInfo);
                }
                return null;
            }

            private EventHandlerInfo( Type eventType, Type interfaceType, TypeInfo interfaceTypeInfo )
            {
                this.EventType = eventType ?? throw Exc.Null(nameof(eventType));
                this.EventTypeInfo = this.EventType.GetTypeInfo();
                this.InterfaceType = interfaceType ?? throw Exc.Null(nameof(interfaceType));
                this.InterfaceTypeInfo = interfaceTypeInfo ?? throw Exc.Null(nameof(interfaceTypeInfo));
            }

            internal Type EventType { get; }
            internal TypeInfo EventTypeInfo { get; }
            internal Type InterfaceType { get; }
            internal TypeInfo InterfaceTypeInfo { get; }

            public override bool Equals( object obj )
            {
                if( obj is EventHandlerInfo info )
                    return this.InterfaceType == info.InterfaceType;
                else
                    return false;
            }

            public override int GetHashCode()
            {
                return this.InterfaceType.GetHashCode();
            }
        }

        #endregion

        #region Private Fields

        private readonly object syncLock = new object();
        private readonly List<ISubscriberWrapper> subscribers = new List<ISubscriberWrapper>();
        private readonly Dictionary<Type, EventHandlerInfo[]> subscriberEventHandlers = new Dictionary<Type, EventHandlerInfo[]>();
        private readonly Dictionary<EventHandlerInfo, WrapperConstructor> weakWrapperConstructors = new Dictionary<EventHandlerInfo, WrapperConstructor>();
        private readonly Dictionary<EventHandlerInfo, WrapperConstructor> strongWrapperConstructors = new Dictionary<EventHandlerInfo, WrapperConstructor>();
        private bool isEnabled = true;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="EventSubscriberCollection"/> class.
        /// </summary>
        public EventSubscriberCollection()
        {
        }

        #endregion

        #region Private Methods

        private bool IsSubscriberAlreadyRegistered( object subscriber, EventHandlerInfo eventHandler )
        {
            for( int i = 0; i < this.subscribers.Count; )
            {
                var wrapper = this.subscribers[i];
                if( CanHandle(wrapper, eventHandler.EventTypeInfo) )
                {
                    switch( AreEqual(wrapper, subscriber) )
                    {
                    case true:
                        // match
                        return true;

                    case false:
                        // mismatch
                        ++i;
                        break;

                    case null:
                        // weak ref. expired
                        this.subscribers.RemoveAt(i);
                        break;
                    }
                }
                else
                {
                    // can not handle event
                    ++i;
                }
            }
            return false;
        }

        private static bool? AreEqual( ISubscriberWrapper wrapper, object subscriber )
        {
            object strongRef = wrapper.AcquireStrongRef();
            if( strongRef.NotNullReference() )
            {
                var result = object.ReferenceEquals(strongRef, subscriber);
                wrapper.ReleaseStrongRef();
                return result;
            }
            else
            {
                return null;
            }
        }

        private static bool CanHandle( ISubscriberWrapper wrapper, TypeInfo eventTypeInfo )
        {
            return eventTypeInfo.IsAssignableFrom(wrapper.EventHandlerInfo.EventTypeInfo);
        }

        private static WrapperConstructor CompileCreateWrapperDelegate( EventHandlerInfo eventHandler, bool useWeakRef )
        {
            var wrapperTypeDefinition = useWeakRef ? typeof(WeakSubscriber<>) : typeof(StrongSubscriber<>);
            var wrapperType = wrapperTypeDefinition.MakeGenericType(eventHandler.EventType);
            var wrapperConstructor = wrapperType.GetTypeInfo().DeclaredConstructors.First();

            var instanceParameter = LambdaExpression.Parameter(typeof(object));
            var infoParameter = LambdaExpression.Parameter(typeof(EventHandlerInfo));
            var body = LambdaExpression.New(
                wrapperConstructor,
                LambdaExpression.Convert(
                    instanceParameter,
                    eventHandler.InterfaceType),
                infoParameter);

            return LambdaExpression.Lambda<WrapperConstructor>(body, instanceParameter, infoParameter).Compile();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Searches implemented <see cref="IEventHandler{T}"/> interfaces,
        /// and registers them if the <paramref name="subscriber"/> is not yet known.
        /// </summary>
        /// <param name="subscriber">The subscriber to search for <see cref="IEventHandler{T}"/> interfaces to store references to.</param>
        /// <param name="useWeakRef"><c>true</c> to only hold weak <see cref="IEventHandler{T}"/> references; <c>false</c> to hold strong references until the subscription is removed.</param>
        /// <returns><c>true</c> if at least one unregistered <see cref="IEventHandler{T}"/> reference was acquired; otherwise, <c>false</c>.</returns>
        public bool Add( object subscriber, bool useWeakRef = true )
        {
            if( subscriber.NullReference() )
                throw Exc.Null(nameof(subscriber));

            lock( this.syncLock )
            {
                if( !this.isEnabled )
                    return false;

                // get the event handlers for the subscriber type
                EventHandlerInfo[] eventHandlers;
                var subscriberType = subscriber.GetType();
                if( !this.subscriberEventHandlers.TryGetValue(subscriberType, out eventHandlers) )
                {
                    eventHandlers = subscriberType
                        .GetTypeInfo()
                        .ImplementedInterfaces
                        .Select(interfaceType => EventHandlerInfo.From(interfaceType))
                        .Where(info => info.NotNullReference())
                        .ToArray();
                    this.subscriberEventHandlers.Add(subscriberType, eventHandlers);
                }

                // find unregistered handlers for this subscriber instance
                bool added = false;
                foreach( var handler in eventHandlers )
                {
                    // is this subscriber-eventHandlerType combination not yet stored?
                    if( !this.IsSubscriberAlreadyRegistered(subscriber, handler) )
                    {
                        // add new subscriber
                        var ctorCollection = useWeakRef ? this.weakWrapperConstructors : this.strongWrapperConstructors;
                        WrapperConstructor createWrapper;
                        if( !ctorCollection.TryGetValue(handler, out createWrapper) )
                        {
                            createWrapper = CompileCreateWrapperDelegate(handler, useWeakRef);
                            ctorCollection.Add(handler, createWrapper);
                        }
                        var wrapperInstance = createWrapper(subscriber, handler);
                        this.subscribers.Add(wrapperInstance);
                        added = true;
                    }
                }

                // false == not an event handler, or already registered.
                return added;
            }
        }

        /// <summary>
        /// Removed all subscriptions of the specified instance.
        /// </summary>
        /// <param name="subscriber">The instance to remove subscriptions of.</param>
        /// <returns><c>true</c> if live subscriptions were found; otherwise, <c>false</c>.</returns>
        public bool Remove( object subscriber )
        {
            if( subscriber.NullReference() )
                throw Exc.Null(nameof(subscriber));

            lock( this.syncLock )
            {
                if( !this.isEnabled )
                    return false;

                bool found = false;
                for( int i = 0; i < this.subscribers.Count; )
                {
                    switch( AreEqual(this.subscribers[i], subscriber) )
                    {
                    case true:
                        // match
                        this.subscribers.RemoveAt(i);
                        found = true;
                        break;

                    case false:
                        // mismatch
                        ++i;
                        break;

                    case null:
                        // weak ref. expired
                        this.subscribers.RemoveAt(i);
                        break;
                    }
                }
                return found;
            }
        }

        /// <summary>
        /// Removes all(!) subscribers who can handle the specified event
        /// (therefore specifying <see cref="EventBase"/> would remove all subscribers,
        /// but <see cref="Clear"/> is better for that).
        /// </summary>
        /// <typeparam name="T">The event to remove all subscribers to.</typeparam>
        /// <returns><c>true</c> if live subscriptions were found; otherwise, <c>false</c>.</returns>
        public bool Remove<T>()
            where T : EventBase
        {
            lock( this.syncLock )
            {
                if( !this.isEnabled )
                    return false;

                bool found = false;
                var eventTypeInfo = typeof(T).GetTypeInfo();
                for( int i = 0; i < this.subscribers.Count; )
                {
                    if( CanHandle(this.subscribers[i], eventTypeInfo) )
                    {
                        this.subscribers.RemoveAt(i);
                        found = true;
                    }
                    else
                    {
                        ++i;
                    }
                }
                return found;
            }
        }

        /// <summary>
        /// Removes all subscribers.
        /// </summary>
        public void Clear()
        {
            lock( this.syncLock )
            {
                if( !this.isEnabled )
                    return;

                this.subscribers.Clear();
            }
        }

        #endregion

        #region Protected Internal Members

        /// <summary>
        /// Finds applicable subscribers, and has them handle the specified event.
        /// </summary>
        /// <param name="evnt">The event to handle.</param>
        /// <param name="exceptions">Stores exceptions throws by event handlers.</param>
        protected internal void Handle( EventBase evnt, List<Exception> exceptions )
        {
            lock( this.syncLock )
            {
                var eventTypeInfo = evnt.GetType().GetTypeInfo();
                for( int i = 0; i < this.subscribers.Count; )
                {
                    var wrapper = this.subscribers[i];
                    if( CanHandle(wrapper, eventTypeInfo) )
                    {
                        bool hasStrongRef = wrapper.AcquireStrongRef().NotNullReference();
                        if( hasStrongRef )
                        {
                            try
                            {
                                wrapper.Handle(evnt);
                            }
                            catch( Exception ex )
                            {
                                exceptions.Add(ex);
                            }

                            wrapper.ReleaseStrongRef();
                            ++i;
                        }
                        else
                        {
                            this.subscribers.RemoveAt(i);
                        }
                    }
                    else
                    {
                        ++i;
                    }
                }
            }
        }

        /// <summary>
        /// Removes current subscribers, and disables adding of new ones.
        /// </summary>
        protected internal void DisableAndClear()
        {
            lock( this.syncLock )
            {
                this.Clear();
                this.isEnabled = false;
            }
        }

        #endregion
    }
}
