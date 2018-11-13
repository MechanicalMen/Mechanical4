using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Mechanical4.EventQueue
{
    /// <summary>
    /// A thread-safe collection of event subscribers.
    /// Once the event queue is shut down,
    /// subscribers are automatically released,
    /// and further method calls will have no effect.
    /// </summary>
    public class EventSubscriberCollection
    {
        //// NOTE: changing the collection, and the handling of events, are mutually exclusive

        #region Event handler wrapping

        //// NOTE: since we may want to use weak references for subscribers
        ////       we will need to store a strong reference to something else.

        //// NOTE: these wrap event handlers, but subscribers may implement more than one of those

        private interface IEventHandlerWrapper
        {
            Type EventType { get; }
            TypeInfo EventTypeInfo { get; }

            bool? ReferenceEquals( object subscriber );
            void Handle( EventBase evnt, out bool referenceFound );
        }

        private class StrongEventHandler<TEvent> : IEventHandlerWrapper
            where TEvent : EventBase
        {
            private readonly IEventHandler<TEvent> strongRef;

            internal StrongEventHandler( IEventHandler<TEvent> subscriber )
            {
                this.strongRef = subscriber ?? throw Exc.Null(nameof(subscriber));
                this.EventType = typeof(TEvent);
                this.EventTypeInfo = this.EventType.GetTypeInfo();
            }

            public Type EventType { get; }
            public TypeInfo EventTypeInfo { get; }

            public bool? ReferenceEquals( object subscriber )
            {
                return ReferenceEquals(this.strongRef, subscriber);
            }

            public void Handle( EventBase evnt, out bool referenceFound )
            {
                this.strongRef.Handle((TEvent)evnt);
                referenceFound = true;
            }
        }

        private class WeakEventHandler<TEvent> : IEventHandlerWrapper
            where TEvent : EventBase
        {
            private readonly WeakReference<IEventHandler<TEvent>> weakRef;

            internal WeakEventHandler( IEventHandler<TEvent> subscriber )
            {
                if( subscriber.NullReference() )
                    throw Exc.Null(nameof(subscriber));

                this.weakRef = new WeakReference<IEventHandler<TEvent>>(subscriber);
                this.EventType = typeof(TEvent);
                this.EventTypeInfo = this.EventType.GetTypeInfo();
            }

            public Type EventType { get; }
            public TypeInfo EventTypeInfo { get; }

            public bool? ReferenceEquals( object subscriber )
            {
                if( this.weakRef.TryGetTarget(out var strongRef) )
                    return ReferenceEquals(strongRef, subscriber);
                else
                    return null;
            }

            public void Handle( EventBase evnt, out bool referenceFound )
            {
                referenceFound = this.weakRef.TryGetTarget(out var strongRef);
                if( referenceFound )
                    strongRef.Handle((TEvent)evnt);
            }
        }

        #endregion

        #region Private Fields

        private readonly object syncLock = new object();
        private readonly List<IEventHandlerWrapper> subscribers = new List<IEventHandlerWrapper>();
        private bool isClosed = false;

        #endregion

        #region Private Methods

        private int IndexOfSubscriber( object subscriber, int startIndex )
        {
            for( int i = startIndex; i < this.subscribers.Count; )
            {
                var wrapper = this.subscribers[i];
                var result = wrapper.ReferenceEquals(subscriber);
                if( result.HasValue )
                {
                    if( result.Value )
                        return i; // return
                    else
                        ++i; // continue
                }
                else
                {
                    // a weak reference that we lost
                    this.subscribers.RemoveAt(i); // remove & continue
                }
            }
            return -1;
        }

        private int IndexOfEventHandler<TEvent>( IEventHandler<TEvent> eventHandler )
            where TEvent : EventBase
        {
            int index, startIndex = 0;
            while( (index = this.IndexOfSubscriber(eventHandler, startIndex)) != -1 )
            {
                var wrapper = this.subscribers[index];
                if( wrapper.EventType == typeof(TEvent) )
                {
                    // reference + event type found
                    return index;
                }
                startIndex = index + 1;
            }
            return -1;
        }

        private static Type[] GetHandledEventTypesOfSubscriber( Type subscriberType )
        {
            return subscriberType
                .GetTypeInfo()
                .ImplementedInterfaces
                .Select(interfaceType => interfaceType.GetTypeInfo())
                .Where(interfaceTypeInfo => interfaceTypeInfo.IsGenericType
                                         && interfaceTypeInfo.GetGenericTypeDefinition() == typeof(IEventHandler<>))
                .Select(interfaceTypeInfo => interfaceTypeInfo.GenericTypeArguments[0])
                .ToArray();
        }

        private static bool CanHandle( IEventHandlerWrapper wrapper, TypeInfo eventTypeInfo )
        {
            return wrapper.EventTypeInfo.IsAssignableFrom(eventTypeInfo);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Registers a new event handler.
        /// Any other event handlers that the <paramref name="eventHandler"/> parameter might implement, will be ignored.
        /// </summary>
        /// <typeparam name="TEvent">The type of event to add a handler for.</typeparam>
        /// <param name="eventHandler">The event handler to add.</param>
        /// <param name="weakRef"><c>true</c> to store a weak reference; <c>false</c> to store a strong reference.</param>
        /// <returns><c>true</c> if this specific event handler was not yet registered; otherwise, <c>false</c>.</returns>
        public bool Add<TEvent>( IEventHandler<TEvent> eventHandler, bool weakRef = true )
            where TEvent : EventBase
        {
            if( eventHandler.NullReference() )
                throw Exc.Null(nameof(eventHandler));

            lock( this.subscribers )
            {
                if( this.isClosed )
                    return false;

                // do nothing if this handler is already registered
                if( this.IndexOfEventHandler(eventHandler) != -1 )
                    return false;

                // wrap handler
                IEventHandlerWrapper newWrapper;
                if( weakRef )
                    newWrapper = new WeakEventHandler<TEvent>(eventHandler);
                else
                    newWrapper = new StrongEventHandler<TEvent>(eventHandler);

                // register wrapper
                this.subscribers.Add(newWrapper);
                return true;
            }
        }

        /// <summary>
        /// Removes the specified event handler.
        /// Any other event handlers that the <paramref name="eventHandler"/> parameter might implement, and may be registered as well, will be ignored.
        /// </summary>
        /// <typeparam name="TEvent">The type of event to remove a handler of.</typeparam>
        /// <param name="eventHandler">The event handler to remove.</param>
        /// <returns><c>true</c> if the event handler was removed; <c>false</c> if it could not be found.</returns>
        public bool Remove<TEvent>( IEventHandler<TEvent> eventHandler )
            where TEvent : EventBase
        {
            if( eventHandler.NullReference() )
                throw Exc.Null(nameof(eventHandler));

            lock( this.subscribers )
            {
                if( this.isClosed )
                    return false;

                int index = this.IndexOfEventHandler(eventHandler);
                if( index != -1 )
                {
                    this.subscribers.RemoveAt(index);
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Searches the subscriber for event handlers it implements,
        /// and adds those that were not yet registered.
        /// </summary>
        /// <param name="subscriber">The subscriber to add the event handlers of.</param>
        /// <param name="weakRef"><c>true</c> to store weak references; <c>false</c> to store strong references.</param>
        /// <returns><c>true</c> if at least one event handler was added; otherwise, <c>false</c>.</returns>
        public bool AddAll( object subscriber, bool weakRef = true )
        {
            if( subscriber.NullReference() )
                throw Exc.Null(nameof(subscriber));

            var eventTypes = GetHandledEventTypesOfSubscriber(subscriber.GetType());
            var parameterList = new object[] { subscriber, weakRef };
            var handlersAdded = false;
            foreach( var t in eventTypes )
            {
                var genericMethodDefinition = typeof(EventSubscriberCollection).GetTypeInfo().GetDeclaredMethod(nameof(Add));
                var method = genericMethodDefinition.MakeGenericMethod(t);
                handlersAdded = (bool)method.Invoke(this, parameterList) || handlersAdded;
            }
            return handlersAdded;
        }

        /// <summary>
        /// Searches the subscriber for event handlers it implements, and removes any that can be found.
        /// </summary>
        /// <param name="subscriber">The subscriber to remove the event handlers of.</param>
        /// <returns><c>true</c> if at least one event handler was removed; <c>false</c> if none could be found.</returns>
        public bool RemoveAll( object subscriber )
        {
            if( subscriber.NullReference() )
                throw Exc.Null(nameof(subscriber));

            var eventTypes = GetHandledEventTypesOfSubscriber(subscriber.GetType());
            var parameterList = new object[] { subscriber };
            var handlersRemoved = false;
            foreach( var t in eventTypes )
            {
                var genericMethodDefinition = typeof(EventSubscriberCollection).GetTypeInfo().GetDeclaredMethod(nameof(Remove));
                var method = genericMethodDefinition.MakeGenericMethod(t);
                handlersRemoved = (bool)method.Invoke(this, parameterList) || handlersRemoved;
            }
            return handlersRemoved;
        }

        /// <summary>
        /// Removes all subscribers.
        /// </summary>
        public void Clear()
        {
            lock( this.syncLock )
            {
                if( this.isClosed )
                    return;

                this.subscribers.Clear();
            }
        }

        #endregion

        #region Internal Members

        /// <summary>
        /// Finds applicable subscribers, and has them handle the specified event, on the current thread.
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
                        bool referenceFound = true;
                        try
                        {
                            wrapper.Handle(evnt, out referenceFound);
                        }
                        catch( Exception ex )
                        {
                            exceptions.Add(ex);
                        }

                        if( referenceFound )
                        {
                            // event handled: continue
                            ++i;
                        }
                        else
                        {
                            // GC already collected the event handler: remove wrapper and continue
                            this.subscribers.RemoveAt(i);
                        }
                    }
                    else
                    {
                        // event handler not compatible with event: continue
                        ++i;
                    }
                }
            }
        }

        /// <summary>
        /// Removes current subscribers, and disables adding of new ones.
        /// </summary>
        internal void DisableAndClear()
        {
            lock( this.syncLock )
            {
                this.Clear();
                this.isClosed = true;
            }
        }

        #endregion
    }
}
