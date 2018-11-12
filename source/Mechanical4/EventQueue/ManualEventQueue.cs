using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Mechanical4.Misc;
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
            ClosingEventEnqueued, // after the closing event was enqueued
            NoNewEventsAccepted, // after the closing event was handled
            Closed // after all events were handled
        }

        private readonly object generalLock = new object();
        private readonly object handleNextLock = new object();
        private readonly List<EventBase> events = new List<EventBase>();
        private readonly List<Exception> eventHandlerExceptions = new List<Exception>();
        private State eventsState = State.Open;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ManualEventQueue"/> class.
        /// </summary>
        /// <param name="subscriberCollection">The <see cref="EventSubscriberCollection"/> to use, or <c>null</c> to create a new one.</param>
        public ManualEventQueue( EventSubscriberCollection subscriberCollection = null )
        {
            this.Subscribers = subscriberCollection ?? new EventSubscriberCollection();
            this.EventHandling = new FeatureSuspender();
            this.EventAdding = new FeatureSuspender();
        }

        #endregion

        #region Private Methods

        private bool ContainsEvent( EventBase evnt )
        {
            bool found = false;
            foreach( var e in this.events )
            {
                if( ReferenceEquals(e, evnt) )
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
            if( this.events.Count != 0 )
            {
                evnt = this.events[0];
                this.events.RemoveAt(0);
            }
            return evnt;
        }

        private bool HasMoreEvents => this.events.Count > 0;

        #endregion

        #region IEventQueue

        /// <summary>
        /// Enqueues an event, to be handled by subscribers sometime later.
        /// There is no guarantee that the event will end up being handled
        /// (e.g. suspended or closed queues silently ignore events,
        /// or the application may be terminated beforehand).
        /// </summary>
        /// <param name="evnt">The event to enqueue.</param>
        /// <param name="file">The source file of the caller.</param>
        /// <param name="member">The method or property name of the caller to this method.</param>
        /// <param name="line">The line number in <paramref name="file"/>.</param>
        /// <returns><c>true</c> if the event was enqueued successfully; otherwise, <c>false</c>.</returns>
        public bool Enqueue(
            EventBase evnt,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
        {
            if( evnt.NullReference() )
                throw Exc.Null(nameof(evnt));

            lock( this.generalLock )
            {
                // already enqueued?
                if( this.ContainsEvent(evnt) )
                    return false;

                // enqueue disabled?
                if( this.EventAdding.IsSuspended
                 || (int)this.eventsState >= (int)State.NoNewEventsAccepted )
                    return false;

                // closing event?
                if( evnt is EventQueueClosingEvent )
                {
                    if( (int)this.eventsState >= (int)State.ClosingEventEnqueued )
                        return false; // another closing event already enqueued
                    else
                        this.eventsState = State.ClosingEventEnqueued;
                }

                evnt.EventEnqueuePos = FileLine.Compact(file, member, line);
                this.events.Add(evnt);
                return true;
            }
        }

        /// <summary>
        /// Gets the collection of event handlers.
        /// </summary>
        public EventSubscriberCollection Subscribers { get; }

        /// <summary>
        /// Gets the object managing whether the handling of events already in the queue can be started.
        /// Does not affect event handling already in progress.
        /// Does not affect the addition of events to the queue (i.e. <see cref="Enqueue"/>).
        /// </summary>
        public FeatureSuspender EventHandling { get; }

        /// <summary>
        /// Gets the object managing whether events are silently discarded, instead of being added to the queue.
        /// This affects neither events already in the queue, nor their handling.
        /// </summary>
        public FeatureSuspender EventAdding { get; }

        #endregion

        #region Public Members

        /// <summary>
        /// Gets a value indicating whether this event queue is closed (see: <see cref="EventQueueClosingEvent"/>).
        /// </summary>
        public bool IsClosed
        {
            get
            {
                lock( this.generalLock )
                    return this.eventsState == State.Closed;
            }
        }

        /// <summary>
        /// Invokes the event handlers of the next event, unless event handling is suspended.
        /// </summary>
        /// <param name="enqueueUnhandledExceptionEvent">If <c>true</c>, unhandled exceptions thrown by event handlers are collected an enqueued as a single <see cref="UnhandledExceptionEvent"/>. If <c>false</c>, all such exceptions are silently ignored.</param>
        /// <returns><c>true</c> if there was an event to handle; <c>false</c> if there was no event available, or event handling was suspended.</returns>
        public bool HandleNext( bool enqueueUnhandledExceptionEvent = true )
        {
            // get next event
            EventBase evnt = null;
            var state = default(State); // keep compiler happy
            lock( this.generalLock )
            {
                if( this.EventHandling.IsEnabled ) // we don't throw exceptions, because we may get suspended after this method is called, but before we check for suspension.
                {
                    evnt = this.TakeNextEvent();
                    state = this.eventsState;
                }
            }
            if( evnt.NullReference() )
                return false;

            // handle event
            Exception exception = null;
            lock( this.handleNextLock )
                exception = EventSubscriberCollection.HandleEvent(evnt, this.Subscribers, this.eventHandlerExceptions);

            // handle exception(s) thrown
            if( exception.NotNullReference()
             && enqueueUnhandledExceptionEvent )
                this.Enqueue(new UnhandledExceptionEvent(exception));

            // did we just handle a closing event?
            if( state == State.ClosingEventEnqueued
             && evnt is EventQueueClosingEvent closingEvent )
            {
                lock( this.generalLock )
                {
                    this.eventsState = State.NoNewEventsAccepted;
                    state = this.eventsState;
                }
            }

            // are we ready to close the event queue?
            if( state == State.NoNewEventsAccepted )
            {
                lock( this.generalLock )
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

            return true;
        }

        #endregion
    }
}
