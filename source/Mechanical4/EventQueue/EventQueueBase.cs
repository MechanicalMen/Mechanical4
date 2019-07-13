using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Mechanical4.EventQueue.Primitives;
using Mechanical4.Misc;

namespace Mechanical4.EventQueue
{
    /// <summary>
    /// A base class implementing <see cref="IEventQueue"/>
    /// </summary>
    public abstract class EventQueueBase : IEventQueue
    {
        #region State

        private enum State
        {
            Open,
            ShuttingDownEnqueued,
            HandlingRemainingEvents, // after the shutting down event was handled
            Shutdown // after all events were handled
        }

        #endregion

        #region HandlingOption

        /// <summary>
        /// Determines how to proceed with handling an event.
        /// </summary>
        protected enum HandlingOption
        {
            /// <summary>
            /// Handle the event as usual.
            /// </summary>
            Handle,

            /// <summary>
            /// The event can not be handled.
            /// Handling will be skipped.
            /// A failure state.
            /// </summary>
            UnableToHandle,

            /// <summary>
            /// The event should be considered handled.
            /// Handling will be skipped.
            /// A success state.
            /// </summary>
            AlreadyHandled
        }

        #endregion

        #region Private Fields

        private readonly object syncLock = new object();
        private readonly ThreadLocal<List<Exception>> eventHandlerExceptions;
        private State eventsState = State.Open;
        private bool shutdownRequestEnqueued = false;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="EventQueueBase"/> class.
        /// </summary>
        /// <param name="eventStorage">The <see cref="IEventQueueStorage"/> to use, or <c>null</c> to use a <see cref="FIFOEventStorage"/>.</param>
        /// <param name="subscriberCollection">The <see cref="EventSubscriberCollection"/> to use, or <c>null</c> to create a new one.</param>
        protected EventQueueBase( IEventQueueStorage eventStorage, EventSubscriberCollection subscriberCollection )
        {
            this.Storage = eventStorage ?? new FIFOEventStorage();
            this.Subscribers = subscriberCollection ?? new EventSubscriberCollection();
            this.EventHandling = new EventQueueFeature(this.OnHandlingSuspended, this.OnHandlingResumed);
            this.EventAdding = new EventQueueFeature();
            this.RaiseUnhandledEvents = new EventQueueFeature();
            this.eventHandlerExceptions = new ThreadLocal<List<Exception>>(() => new List<Exception>());
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
            lock( this.syncLock )
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
        public EventQueueFeature EventHandling { get; }

        /// <summary>
        /// Gets the object managing whether events are silently discarded, instead of being added to the queue.
        /// This affects neither events already in the queue, nor their handling.
        /// </summary>
        public EventQueueFeature EventAdding { get; }

        /// <summary>
        /// Gets the object managing whether exceptions thrown by event handlers
        /// are wrapped and raised as <see cref="UnhandledExceptionEvent"/>.
        /// <see cref="EventAdding"/> must be enabled for this to work.
        /// </summary>
        public EventQueueFeature RaiseUnhandledEvents { get; }

        #endregion

        #region Private Members

        private bool EnqueueIgnoringSuspension(
            EventBase evnt,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
        {
            this.BeforeAdding(evnt, file, member, line, out bool canAdd);
            if( !canAdd )
                return false;

            var eventAdded = this.Storage.TryPush(evnt);

            if( eventAdded )
                this.AfterAdding(evnt);
            return eventAdded;
        }

        #endregion

        #region Protected Members

        /// <summary>
        /// Gets the <see cref="IEventQueueStorage"/> being used.
        /// </summary>
        protected IEventQueueStorage Storage { get; }

        /// <summary>
        /// Invoked before the event is added to the <see cref="IEventQueueStorage"/>.
        /// Can optionally cancel the the adding.
        /// </summary>
        /// <param name="evnt">The event about to be added to the queue.</param>
        /// <param name="file">The source file of the caller.</param>
        /// <param name="member">The method or property name of the caller to this method.</param>
        /// <param name="line">The line number in <paramref name="file"/>.</param>
        /// <param name="canAdd">Determines whether the event can be added to the queue.</param>
        protected virtual void BeforeAdding(
            EventBase evnt,
            string file,
            string member,
            int line,
            out bool canAdd )
        {
            if( evnt.NullReference() )
                throw Exc.Null(nameof(evnt));

            lock( this.syncLock )
            {
                // already enqueued?
                if( this.Storage.Contains(evnt) )
                {
                    canAdd = false;
                    return;
                }

                // shutdown request event?
                if( evnt is ShutdownRequestEvent )
                {
                    if( this.shutdownRequestEnqueued // another request is already enqueued or being handled
                     || (this.eventsState >= State.ShuttingDownEnqueued) ) // we are already shutting down, there is no question about it
                    {
                        canAdd = false;
                        return;
                    }
                    else
                    {
                        this.shutdownRequestEnqueued = true;
                    }
                }
                else if( evnt is ShuttingDownEvent ) // shutting down event?
                {
                    if( this.eventsState >= State.ShuttingDownEnqueued )
                    {
                        // another shutting down event already enqueued
                        canAdd = false;
                        return;
                    }
                    else
                    {
                        this.eventsState = State.ShuttingDownEnqueued;
                    }
                }

                evnt.EventEnqueuePos = FileLine.Compact(file, member, line);
                canAdd = true;
            }
        }

        /// <summary>
        /// Invoked after the event was successfully added to the <see cref="IEventQueueStorage"/>.
        /// </summary>
        /// <param name="evnt">The event that was added.</param>
        protected virtual void AfterAdding( EventBase evnt )
        {
        }

        /// <summary>
        /// Invoked before the event is handled.
        /// Can optionally cancel the handling.
        /// </summary>
        /// <param name="evnt">The event to handle.</param>
        /// <returns>A <see cref="HandlingOption"/> value determining what to do with the event.</returns>
        protected virtual HandlingOption BeforeHandling( EventBase evnt )
        {
            if( evnt.NullReference() )
                return HandlingOption.UnableToHandle;

            lock( this.syncLock )
            {
                // skip handling shutdown requests, if a shutting down event was already enqueued
                if( this.eventsState >= State.ShuttingDownEnqueued
                 && evnt is ShutdownRequestEvent )
                {
                    // there was an event found, and event handling was not suspended.
                    return HandlingOption.AlreadyHandled;
                }
            }

            return HandlingOption.Handle;
        }

        /// <summary>
        /// Invoked after the event was handled.
        /// </summary>
        /// <param name="evnt">The event that was handled.</param>
        protected virtual void AfterHandling( EventBase evnt )
        {
            // was it a shutdown request?
            if( evnt is ShutdownRequestEvent requestEvent )
            {
                lock( this.syncLock )
                {
                    this.shutdownRequestEnqueued = false;

                    // unless it was cancelled, we begin shutting down
                    if( !requestEvent.Cancel )
                        this.EnqueueIgnoringSuspension(new ShuttingDownEvent()); // this is a special case where we ignore suspension of adding
                }
            }
            else if( evnt is ShuttingDownEvent ) // was it a shutting down event?
            {
                lock( this.syncLock )
                {
                    this.EventAdding.Suspend();
                    this.eventsState = State.HandlingRemainingEvents;
                    this.EnqueueIgnoringSuspension(new ShutDownEvent()); // this is a special case where we ignore suspension of adding
                }
            }
            else if( evnt is ShutDownEvent ) // was it the last event?
            {
                lock( this.syncLock )
                {
                    if( !this.Storage.IsEmpty )
                        throw new Exception("Invalid queue state: there should be no more events left! Did the event adding suppression somehow reset?");

                    // no new events will be added, and all events were handled:
                    // remove current subscribers and do not accept others
                    this.Subscribers.DisableAndClear();

                    // set state to finished
                    this.eventsState = State.Shutdown;
                }
            }
        }

        /// <summary>
        /// Invoked when the queue is suspended.
        /// </summary>
        protected virtual void OnHandlingSuspended()
        {
        }

        /// <summary>
        /// Invoked when the queue is resumed.
        /// </summary>
        protected virtual void OnHandlingResumed()
        {
        }

        /// <summary>
        /// Tries to invoke the event handlers of the next event in the storage.
        /// May fail if the event storage is empty, event handling is suspended,
        /// or if <see cref="EventQueueBase.BeforeHandling"/> returned <see cref="EventQueueBase.HandlingOption.UnableToHandle"/>.
        /// </summary>
        /// <param name="evnt">The event that was handled; or <c>null</c>.</param>
        /// <param name="exceptions">An optional list used to store exceptions thrown by event handlers. It always stores the exceptions, independent of the current state of <see cref="IEventQueue.RaiseUnhandledEvents"/>.</param>
        /// <returns><c>true</c> if an event was handled successfully; otherwise, <c>false</c>.</returns>
        protected bool TryHandleNextEvent( out EventBase evnt, List<Exception> exceptions = null )
        {
            if( this.EventHandling.IsEnabled )
            {
                evnt = this.Storage.TryPop(); // thread-safe
                if( evnt.NotNullReference() )
                    return this.TryHandleEventIgnoringSuspension(evnt, exceptions);
            }

            evnt = null;
            return false;
        }

        /// <summary>
        /// Tries to invoke the event handlers of the specified event.
        /// This event does not have to have been enqueued beforehand, the storage is not checked.
        /// <see cref="IEventQueue.EventHandling"/> is ignored.
        /// May fail if <see cref="EventQueueBase.BeforeHandling"/> returned <see cref="EventQueueBase.HandlingOption.UnableToHandle"/>.
        /// </summary>
        /// <param name="eventToHandle">The specific event to have handled, or <c>null</c></param>
        /// <param name="exceptions">An optional list used to store exceptions thrown by event handlers. It always stores the exceptions, independent of the current state of <see cref="IEventQueue.RaiseUnhandledEvents"/>.</param>
        /// <returns><c>true</c> if an event was handled successfully; otherwise, <c>false</c>.</returns>
        protected internal bool TryHandleEventIgnoringSuspension( EventBase eventToHandle, List<Exception> exceptions = null )
        {
            if( eventToHandle.NullReference() )
                return false;

            if( exceptions.NullReference() )
                exceptions = this.eventHandlerExceptions.Value;

            // prepare for event handling
            lock( this.syncLock )
            {
                // clear exception list
                exceptions.Clear(); // if passed by parameter, it may not be thread-safe, so we lock it

                // pre-handling
                var handleOption = this.BeforeHandling(eventToHandle);
                switch( handleOption )
                {
                case HandlingOption.UnableToHandle:
                    return false;

                case HandlingOption.AlreadyHandled:
                    return true;

                case HandlingOption.Handle:
                    //// see below
                    break;

                default:
                    throw new Exception($"Unable to handle option: {handleOption}!");
                }
            }

            // let event handlers get to work
            // (NOTE: if we used a not thread-local exception list, a concurrent Handle call could clear it midway through)
            this.Subscribers.Handle(eventToHandle, exceptions); // thread-safe

            // cleanup
            lock( this.syncLock )
            {
                // deal with exceptions thrown
                if( exceptions.Count != 0
                 && this.RaiseUnhandledEvents.IsEnabled )
                {
                    for( int i = 0; i < exceptions.Count; ++i )
                    {
                        this.Enqueue(
                            new UnhandledExceptionEvent(
                                exceptions[i]
                                    .Store("originalEventAddedFrom", eventToHandle.EventEnqueuePos))); // addition may be suspended, or that functionality may have been changed by one of the handlers, we don't care
                    }
                }

                // post-handling
                this.AfterHandling(eventToHandle);
                return true;
            }
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Gets a value indicating whether this event queue was shut down.
        /// </summary>
        public bool IsShutDown
        {
            get
            {
                lock( this.syncLock )
                    return this.eventsState == State.Shutdown;
            }
        }

        #endregion
    }
}
