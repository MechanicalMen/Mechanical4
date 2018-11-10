using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Mechanical4.Core;
using Mechanical4.Core.Misc;
using Mechanical4.EventQueue.Events;

namespace Mechanical4.EventQueue
{
    /// <summary>
    /// A thread-safe implementation of <see cref="IEventQueue"/>.
    /// Events are stored, until they are handled one-by-on using <see cref="HandleNext"/>.
    /// Events are handled on the thread calling <see cref="HandleNext"/>, not the one calling <see cref="Enqueue"/>.
    /// </summary>
    public class ManualEventQueue : IEventQueue
    {
        #region Private Fields

        private enum State : int
        {
            Open,
            ClosingEventEnqueued,
            NoNewEventsAccepted,
            Closed
        }

        private readonly object eventsLock = new object();
        private readonly object eventHandlingLock = new object();
        private readonly List<EventBase> normalEvents = new List<EventBase>();
        private readonly List<Exception> eventHandlerExceptions = new List<Exception>();
        private State eventsState = State.Open;
        private bool isSuspended = false;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ManualEventQueue"/> class.
        /// </summary>
        public ManualEventQueue()
        {
            this.Subscribers = new EventSubscriberCollection();
        }

        #endregion

        #region Private Methods

        private bool ContainsEvent( EventBase evnt )
        {
            bool found = false;
            foreach( var e in this.normalEvents )
            {
                if( object.ReferenceEquals(e, evnt) )
                {
                    found = true;
                    break;
                }
            }

            return found;
        }

        private EventBase TakeNextEvent()
        {
            EventBase evnt = null;
            if( this.normalEvents.Count != 0 )
            {
                evnt = this.normalEvents[0];
                this.normalEvents.RemoveAt(0);
            }
            return evnt;
        }

        private bool HasMoreEvents => this.normalEvents.Count > 0;

        #endregion

        #region Internal Methods

        internal static void SetMetaData( EventBase evnt, string file, string member, int line )
        {
            evnt.EventEnqueuePos = FileLine.Compact(file, member, line);
        }

        internal static Exception HandleEvent( EventBase evnt, EventSubscriberCollection subscribers, List<Exception> eventHandlerExceptions )
        {
            subscribers.Handle(evnt, eventHandlerExceptions);
            if( eventHandlerExceptions.Count != 0 )
            {
                var exception = new AggregateException(
                    $"Unhandled exception(s) thrown, while handling an event ({evnt.ToString()}). The event was enqueued at: {evnt.EventEnqueuePos}.",
                    eventHandlerExceptions.ToArray());
                eventHandlerExceptions.Clear();
                return exception;
            }
            else
            {
                return null;
            }
        }

        #endregion

        #region IEventQueue

        /// <summary>
        /// Enqueues an event, to be handled by subscribers sometime later.
        /// There is no guarantee that the event will end up being handled
        /// (e.g. closed queues can not enqueue, and the application
        /// may be terminated beforehand).
        /// Suspended event queues can still enqueue events (see <see cref="IsSuspended"/>).
        /// </summary>
        /// <param name="evnt">The event to enqueue.</param>
        /// <param name="file">The source file of the caller.</param>
        /// <param name="member">The method or property name of the caller to this method.</param>
        /// <param name="line">The line number in <paramref name="file"/>.</param>
        public void Enqueue(
            EventBase evnt,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
        {
            if( evnt.NullReference() )
                throw Exc.Null(nameof(evnt));

            if( evnt is ICriticalEvent )
                throw new ArgumentException("This event queue can not handle critical events!").StoreFileLine();

            lock( this.eventsLock )
            {
                // already enqueued?
                if( this.ContainsEvent(evnt) )
                    return;

                // enqueue disabled?
                if( (int)this.eventsState >= (int)State.NoNewEventsAccepted )
                    return;

                // closing event?
                if( evnt is EventQueueClosingEvent )
                {
                    if( (int)this.eventsState >= (int)State.ClosingEventEnqueued )
                        return; // another closing event already enqueued
                    else
                        this.eventsState = State.ClosingEventEnqueued;
                }

                SetMetaData(evnt, file, member, line);
                this.normalEvents.Add(evnt);
            }
        }

        /// <summary>
        /// Gets the collection of event handlers.
        /// </summary>
        public EventSubscriberCollection Subscribers { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the handling of enqueued events is currently allowed.
        /// Suspension does not apply to event handling already in progress.
        /// </summary>
        public bool IsSuspended
        {
            get
            {
                lock( this.eventsLock )
                    return this.isSuspended;
            }
            set
            {
                lock( this.eventsLock )
                    this.isSuspended = value;
            }
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Gets a value indicating whether this event queue is closed (see: <see cref="EventQueueClosingEvent"/>).
        /// </summary>
        public bool IsClosed
        {
            get
            {
                lock( this.eventsLock )
                    return this.eventsState == State.Closed;
            }
        }

        /// <summary>
        /// Invokes the event handlers of the next event.
        /// </summary>
        /// <param name="enqueueUnhandledExceptionEvent">If <c>true</c>, unhandled exceptions thrown by event handlers are collected an enqueued as a single <see cref="UnhandledExceptionEvent"/>. If <c>false</c>, all such exceptions are silently ignored.</param>
        /// <returns><c>true</c> if there was an event to handle; <c>false</c> if there was no event available.</returns>
        public bool HandleNext( bool enqueueUnhandledExceptionEvent = true )
        {
            // get next event
            EventBase evnt = null;
            var state = default(State); // keep compiler happy
            lock( this.eventsLock )
            {
                if( !this.IsSuspended ) // we don't throw exceptions, because we may get suspended after this method is called, but before we check for suspension.
                {
                    evnt = this.TakeNextEvent();
                    state = this.eventsState;
                }
            }
            if( evnt.NullReference() )
                return false;

            // handle event
            Exception exception = null;
            lock( this.eventHandlingLock )
                exception = HandleEvent(evnt, this.Subscribers, this.eventHandlerExceptions);

            // event queue closing?
            if( state == State.ClosingEventEnqueued
             && evnt is EventQueueClosingEvent closingEvent )
            {
                lock( this.eventsLock )
                {
                    this.eventsState = State.NoNewEventsAccepted;
                    state = this.eventsState;
                }
            }

            // event queue closed?
            if( state == State.NoNewEventsAccepted )
            {
                lock( this.eventsLock )
                {
                    if( !this.HasMoreEvents )
                    {
                        // no new events will be added, and all events were handled:
                        // remove current subscribers and do not accept others
                        this.Subscribers.DisableAndClear();

                        this.eventsState = State.Closed;
                        state = this.eventsState;
                    }
                }
            }

            // handle exception
            if( exception.NotNullReference()
             && enqueueUnhandledExceptionEvent )
                this.Enqueue(new UnhandledExceptionEvent(exception));

            return true;
        }

        #endregion
    }
}
