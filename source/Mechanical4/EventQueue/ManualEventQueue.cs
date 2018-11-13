using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Mechanical4.Misc;

namespace Mechanical4.EventQueue
{
    /// <summary>
    /// A thread-safe implementation of <see cref="IEventQueue"/>.
    /// Events are stored, until they are handled one-by-on using <see cref="HandleNext()"/>.
    /// Events are handled on the thread calling <see cref="HandleNext()"/>, not the one calling <see cref="Enqueue"/>.
    /// </summary>
    public class ManualEventQueue : IEventQueue
    {
        #region Private Fields

        private enum State : int
        {
            Open,
            ShuttingDownEnqueued,
            HandlingRemainingEvents, // after the shutting down event was handled
            Shutdown // after all events were handled
        }

        private readonly object generalLock = new object();
        private readonly object handleNextLock = new object();
        private readonly List<EventBase> events = new List<EventBase>();
        private readonly List<Exception> eventHandlerExceptions = new List<Exception>();
        private State eventsState = State.Open;
        private bool shutdownRequestEnqueued = false;

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
            this.RaiseUnhandledEvents = new FeatureSuspender();
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

        private bool EnqueueIgnoringSuspension(
            EventBase evnt,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
        {
            // already enqueued?
            if( this.ContainsEvent(evnt) )
                return false;

            // shutdown request event?
            if( evnt is ShutdownRequestEvent )
            {
                if( this.shutdownRequestEnqueued // another request is already enqueued or being handled
                 || ((int)this.eventsState >= (int)State.ShuttingDownEnqueued) ) // we are already shutting down, there is no question about it
                    return false;
                else
                    this.shutdownRequestEnqueued = true;
            }
            else if( evnt is ShuttingDownEvent ) // shutting down event?
            {
                if( (int)this.eventsState >= (int)State.ShuttingDownEnqueued )
                    return false; // another shutting down event already enqueued
                else
                    this.eventsState = State.ShuttingDownEnqueued;
            }

            evnt.EventEnqueuePos = FileLine.Compact(file, member, line);
            this.events.Add(evnt);
            return true;
        }

        #endregion

        #region IEventQueue

        /// <summary>
        /// Enqueues an event, to be handled by subscribers sometime later.
        /// There is no guarantee that the event will end up being handled
        /// (e.g. suspended or shut down queues silently ignore events,
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
                // enqueue disabled?
                if( this.EventAdding.IsSuspended )
                    return false;

                return this.EnqueueIgnoringSuspension(evnt, file, member, line);
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

        /// <summary>
        /// Gets the object managing whether exceptions thrown by event handlers
        /// are wrapped and raised as <see cref="UnhandledExceptionEvent"/>.
        /// <see cref="EventAdding"/> must be enabled for this to work.
        /// </summary>
        public FeatureSuspender RaiseUnhandledEvents { get; }

        #endregion

        #region Public Members

        /// <summary>
        /// Gets a value indicating whether this event queue was shut down.
        /// </summary>
        public bool IsShutDown
        {
            get
            {
                lock( this.generalLock )
                    return this.eventsState == State.Shutdown;
            }
        }

        /// <summary>
        /// Invokes the event handlers of the next event, unless event handling is suspended.
        /// </summary>
        /// <returns><c>true</c> if there was an event to handle; <c>false</c> if there was no event available, or event handling was suspended.</returns>
        public bool HandleNext()
        {
            return this.HandleNext(this.eventHandlerExceptions);
        }

        /// <summary>
        /// Invokes the event handlers of the next event, unless event handling is suspended.
        /// </summary>
        /// <param name="exceptions">The list to store exceptions thrown by event handlers in. Does not affect the behavior of <see cref="RaiseUnhandledEvents"/>.</param>
        /// <returns><c>true</c> if there was an event to handle; <c>false</c> if there was no event available, or event handling was suspended.</returns>
        public bool HandleNext( List<Exception> exceptions )
        {
            if( exceptions.NullReference() )
                throw Exc.Null(nameof(exceptions));

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

            // skip handling shutdown requests, if a shutting down event was already enqueued
            if( (int)state >= (int)State.ShuttingDownEnqueued
             && evnt is ShutdownRequestEvent )
            {
                exceptions.Clear();
                return true; // there was an event found, and event handling was not suspended.
            }

            // handle event and exceptions
            lock( this.handleNextLock )
            {
                // let handlers get to work
                exceptions.Clear();
                this.Subscribers.Handle(evnt, exceptions);

                // deal with exceptions thrown
                if( exceptions.Count != 0
                 && this.RaiseUnhandledEvents.IsEnabled )
                {
                    for( int i = 0; i < exceptions.Count; ++i )
                        this.Enqueue(new UnhandledExceptionEvent(exceptions[i])); // addition may be suspended, or the functionality may be changed in the middle, we don't care
                }
            }

            // was it a shutdown request?
            if( evnt is ShutdownRequestEvent requestEvent )
            {
                lock( this.generalLock )
                {
                    this.shutdownRequestEnqueued = false;

                    // unless it was cancelled, we begin shutting down
                    if( !requestEvent.Cancel )
                        this.EnqueueIgnoringSuspension(new ShuttingDownEvent()); // this is a special case where we ignore suspension of adding
                }
            }
            else if( evnt is ShuttingDownEvent ) // was it a shutting down event?
            {
                lock( this.generalLock )
                {
                    this.EventAdding.Suspend();
                    this.eventsState = State.HandlingRemainingEvents;
                    this.EnqueueIgnoringSuspension(new ShutDownEvent()); // this is a special case where we ignore suspension of adding
                    state = this.eventsState;
                }
            }
            else if( evnt is ShutDownEvent ) // was it the last event?
            {
                lock( this.generalLock )
                {
                    if( this.HasMoreEvents )
                        throw new Exception("Invalid queue state: there should be no more events left! Did the event adding suppression somehow reset?");

                    // no new events will be added, and all events were handled:
                    // remove current subscribers and do not accept others
                    this.Subscribers.DisableAndClear();
                    state = this.eventsState = State.Shutdown;
                }
            }

            return true;
        }

        #endregion
    }
}
