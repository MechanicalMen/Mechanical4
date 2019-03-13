using System;
using System.Collections.Generic;
using System.Reflection;

namespace Mechanical4.EventQueue.Primitives
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
        //// NOTE: event subscribers may implement more than one event handler

        #region Private Fields

        private readonly object syncLock = new object();
        private readonly List<EventHandlerWrapper.IEventHandlerWrapper> subscribers = new List<EventHandlerWrapper.IEventHandlerWrapper>();
        private bool isClosed = false;

        #endregion

        #region Private Methods

        private int IndexOfFirstEventHandler( object subscriber, int startIndex )
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
                    this.subscribers.RemoveAt(i); // remove & continue with the next item
                }
            }
            return -1;
        }

        private int IndexOfSpecificEventHandler<TEvent>( IEventHandler<TEvent> eventHandler ) // reference equality is ambiguous if the subscriber implements more than one event handler
            where TEvent : EventBase
        {
            int index, startIndex = 0;
            while( (index = this.IndexOfFirstEventHandler(eventHandler, startIndex)) != -1 )
            {
                var wrapper = this.subscribers[index];
                if( wrapper.EventHandlerEventType == typeof(TEvent) )
                {
                    // reference + event type found
                    return index;
                }
                startIndex = index + 1;
            }
            return -1;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Registers a new event handler.
        /// Any other event handlers that the <paramref name="eventHandler"/> instance might implement, will be ignored.
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
                if( this.IndexOfSpecificEventHandler<TEvent>(eventHandler) != -1 )
                    return false;

                // wrap handler
                var newWrapper = EventHandlerWrapper.Create<TEvent>(eventHandler, weakRef);

                // register wrapper
                this.subscribers.Add(newWrapper);
                return true;
            }
        }

        /// <summary>
        /// Removes the specified event handler.
        /// Any other event handlers that the <paramref name="eventHandler"/> instance might implement,
        /// which may or may not be registered as well, will be ignored.
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

                int index = this.IndexOfSpecificEventHandler<TEvent>(eventHandler);
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

            var subscriberTypeInfo = EventSubscriberTypeInfo.AddOrGet(subscriber.GetType());
            return subscriberTypeInfo.AddAll(this, subscriber, weakRef);
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

            var subscriberTypeInfo = EventSubscriberTypeInfo.AddOrGet(subscriber.GetType());
            return subscriberTypeInfo.RemoveAll(this, subscriber);
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
            if( evnt.NullReference() )
                throw Exc.Null(nameof(evnt));

            if( exceptions.NullReference() )
                throw Exc.Null(nameof(exceptions));

            lock( this.syncLock )
            {
                var eventTypeInfo = evnt.GetType().GetTypeInfo();
                for( int i = 0; i < this.subscribers.Count; )
                {
                    var wrapper = this.subscribers[i];
                    if( EventBase.CanHandle(wrapper.EventHandlerEventTypeInfo, eventTypeInfo) )
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
