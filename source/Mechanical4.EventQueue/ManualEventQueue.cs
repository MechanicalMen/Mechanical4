using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using Mechanical4.EventQueue.Events;
using Mechanical4.EventQueue.Serialization;
using Mechanical4.Core;

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

        private static readonly char[] FilePathSeparators = new char[] { '\\', '/' };

        private readonly object eventsLock = new object();
        private readonly object eventHandlingLock = new object();
        private readonly List<EventBase> normalEvents = new List<EventBase>();
        private readonly List<EventBase> criticalEvents = new List<EventBase>();
        private readonly List<Exception> eventHandlerExceptions = new List<Exception>();
        private State eventsState = State.Open;

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

            if( !found )
            {
                foreach( var e in this.criticalEvents )
                {
                    if( object.ReferenceEquals(e, evnt) )
                    {
                        found = true;
                        break;
                    }
                }
            }

            return found;
        }

        private static string ToSourceCodePosition( string file, string member, int line )
        {
            string getFileName( string str )
            {
                int separator = str?.LastIndexOfAny(FilePathSeparators) ?? -1;
                if( separator == -1 )
                    return str;
                else
                    return str.Substring(startIndex: separator + 1); // this works even if the separator is the last character
            }

            return string.Format(CultureInfo.InvariantCulture, "{0}, {1}:{2}", member, getFileName(file), line);
        }

        private EventBase TakeNextEvent()
        {
            EventBase evnt = null;
            if( this.criticalEvents.Count != 0 )
            {
                evnt = this.criticalEvents[0];
                this.criticalEvents.RemoveAt(0);
            }
            else if( this.normalEvents.Count != 0 )
            {
                evnt = this.normalEvents[0];
                this.normalEvents.RemoveAt(0);
            }
            return evnt;
        }

        private bool HasMoreEvents => this.normalEvents.Count + this.criticalEvents.Count > 0;

        private Exception HandleEvent( EventBase evnt )
        {
            this.Subscribers.Handle(evnt, this.eventHandlerExceptions);
            if( this.eventHandlerExceptions.Count != 0 )
            {
                var exception = new AggregateException(
                    $"Unhandled exception(s) thrown, while handling an event ({evnt.ToString()}). The event was enqueued at: {evnt.EventEnqueuePos}.",
                    this.eventHandlerExceptions.ToArray());
                this.eventHandlerExceptions.Clear();
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
        /// (e.g. closed queues can not enqueue, and critical closing events
        /// disable non-critical event handling, see <see cref="EventQueueClosingEvent.IsCritical"/>).
        /// </summary>
        /// <param name="evnt">The event to enqueue.</param>
        /// <param name="critical"><c>true</c> if the event needs to be handled before other non-critical events; otherwise, <c>false</c>.</param>
        /// <param name="file">The source file of the caller.</param>
        /// <param name="member">The method or property name of the caller to this method.</param>
        /// <param name="line">The line number in <paramref name="file"/>.</param>
        public void Enqueue(
            EventBase evnt,
            bool critical = false,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
        {
            if( evnt.NullReference() )
                throw Exc.Null(nameof(evnt));

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

                evnt.EventEnqueuePos = ToSourceCodePosition(file, member, line);
                if( evnt is SerializableEventBase se )
                    se.EventEnqueueTime = DateTime.UtcNow;

                if( critical )
                    this.criticalEvents.Add(evnt);
                else
                    this.normalEvents.Add(evnt);
            }
        }

        /// <summary>
        /// Gets the collection of event handlers.
        /// </summary>
        public EventSubscriberCollection Subscribers { get; }

        #endregion

        #region Public Members

        /// <summary>
        /// Enqueues an <see cref="EventQueueClosingEvent"/>.
        /// Critical closing events indicate that the application is being forced to terminate (see <see cref="EventQueueClosingEvent.IsCritical"/>),
        /// and that the queue should only handle critical events.
        /// </summary>
        /// <param name="critical"><c>true</c> if the application is being forced to terminate; otherwise, <c>false</c>.</param>
        /// <param name="file">The source file of the caller.</param>
        /// <param name="member">The method or property name of the caller to this method.</param>
        /// <param name="line">The line number in <paramref name="file"/>.</param>
        public void BeginClose(
            bool critical = false,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
        {
            this.Enqueue(
                new EventQueueClosingEvent(critical),
                critical,
                file,
                member,
                line);
        }

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
            State state;
            lock( this.eventsLock )
            {
                evnt = this.TakeNextEvent();
                state = this.eventsState;
            }
            if( evnt.NullReference() )
                return false;

            // handle event
            Exception exception = null;
            lock( this.eventHandlingLock )
                exception = this.HandleEvent(evnt);

            // event queue closing?
            if( state == State.ClosingEventEnqueued
             && evnt is EventQueueClosingEvent closingEvent )
            {
                lock( this.eventsLock )
                {
                    this.eventsState = State.NoNewEventsAccepted;
                    state = this.eventsState;

                    // critical closing event?
                    // (do not handle non-critical events!)
                    if( closingEvent.IsCritical )
                        this.normalEvents.Clear();
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
                this.Enqueue(new UnhandledExceptionEvent(exception), critical: false);

            return true;
        }

        #endregion
    }
}
