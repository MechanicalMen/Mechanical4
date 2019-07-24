using System;
using System.Runtime.CompilerServices;
using Mechanical4.EventQueue;
using Mechanical4.EventQueue.Primitives;
using Mechanical4.Misc;

namespace Mechanical4.MVVM
{
    /// <summary>
    /// Wraps n <see cref="EventQueueBase"/>, and provides a way to have critical events handled by it.
    /// Event queue shutdown will trigger <see cref="AppState.Shutdown"/>, and vica versa.
    /// </summary>
    public class MainEventQueue : IEventQueue,
                                  IEventHandler<ShuttingDownEvent>
    {
        #region Private Fields

        private readonly object criticalEventHandlingLock = new object();
        private readonly object moveToStateLock = new object();
        private readonly EventQueueBase regularEventQueue;
        private readonly ThreadSafeEnum<AppState> currentAppState;

        #endregion

        #region Constructors

        internal MainEventQueue( EventQueueBase eventQueue )
        {
            if( eventQueue.NullReference() )
                throw Exc.Null(nameof(eventQueue));

            if( eventQueue.IsShutDown )
                throw new ArgumentException("Event queue specified may not be shut down!");

            this.regularEventQueue = eventQueue;
            this.regularEventQueue.Subscribers.Add<ShuttingDownEvent>(this, weakRef: false);
            this.currentAppState = new ThreadSafeEnum<AppState>(AppState.Shutdown);
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
        bool IEventQueue.Enqueue( EventBase evnt, string file, string member, int line )
        {
            return this.EnqueueRegular(evnt, file, member, line);
        }

        /// <summary>
        /// Gets the collection of event handlers.
        /// </summary>
        public EventSubscriberCollection Subscribers => this.regularEventQueue.Subscribers;

        /// <summary>
        /// Gets the object managing whether the handling of events already in the queue can be started.
        /// Does not affect event handling already in progress.
        /// Does not affect the addition of events to the queue (i.e. <see cref="IEventQueue.Enqueue"/>).
        /// </summary>
        public EventQueueFeature EventHandling => this.regularEventQueue.EventHandling;

        /// <summary>
        /// Gets the object managing whether events are silently discarded, instead of being added to the queue.
        /// This affects neither events already in the queue, nor their handling.
        /// </summary>
        public EventQueueFeature EventAdding => this.regularEventQueue.EventAdding;

        /// <summary>
        /// Gets the object managing whether exceptions thrown by event handlers
        /// are wrapped and raised as <see cref="UnhandledExceptionEvent"/>.
        /// <see cref="EventAdding"/> must be enabled for this to work.
        /// </summary>
        public EventQueueFeature RaiseUnhandledEvents => this.regularEventQueue.RaiseUnhandledEvents;

        #endregion

        #region Private Methods

        /// <summary>
        /// Handles the specified event.
        /// </summary>
        /// <param name="evnt">The event to handle.</param>
        void IEventHandler<ShuttingDownEvent>.Handle( ShuttingDownEvent evnt )
        {
            // make sure critical shutdown state is reached before the event queue finishes shutting down
            if( this.LastStateFinishedHandling != AppState.Shutdown )
            {
                FileLine.ParseCompact(evnt.EventEnqueuePos, out var file, out var member, out var line);
                this.MoveToState(AppState.Shutdown, file, member, line);
            }
        }

        #endregion

        #region Internal Members

        internal AppState LastStateFinishedHandling
        {
            get => this.currentAppState.GetCopy();
            private set => this.currentAppState.Set(value);
        }

        internal void MoveToState( AppState newState, string file, string member, int line )
        {
            if( !Enum.IsDefined(typeof(AppState), newState) )
                throw new ArgumentException($"Invalid value! ({newState})");

            this.EventHandling.Suspend(); // do not allow regular event handling to flicker on, between multiple state changes
            try
            {
                lock( this.moveToStateLock ) // only one state change at a time
                {
                    bool stateChanged = false;
                    while( true )
                    {
                        // are we done?
                        var currState = this.LastStateFinishedHandling;
                        if( currState == newState )
                            break;

                        // determine the next state to switch to
                        AppState nextState;
                        if( newState < currState )
                            nextState = (AppState)((int)currState - 1);
                        else
                            nextState = (AppState)((int)currState + 1);

                        // switch to the next state
                        this.HandleCritical(
                            new AppStateChangedEvent.Critical(
                                oldState: currState,
                                newState: nextState),
                            file,
                            member,
                            line);

                        // update state
                        this.LastStateFinishedHandling = nextState;
                        stateChanged = true;
                    }

                    if( stateChanged )
                    {
                        // we reached the target state, enqueue a regular event
                        this.EnqueueRegular(new AppStateChangedEvent.Regular(this));

                        // if we reached the shutdown state, make sure we begin to shut down the event queue
                        if( newState == AppState.Shutdown )
                            this.EnqueueRegular(new ShuttingDownEvent()); // may not get enqueued, if the queue is already shutting down
                    }
                }
            }
            finally
            {
                this.EventHandling.Resume();
            }
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Gets the event queue being wrapped.
        /// </summary>
        public EventQueueBase BaseEventQueue => this.regularEventQueue;

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
        public bool EnqueueRegular(
            EventBase evnt,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
        {
            if( evnt is ICriticalEvent )
                throw new ArgumentException($"{evnt.GetType().FullName} is a critical event!");

            return this.regularEventQueue.Enqueue(evnt, file, member, line);
        }

        /// <summary>
        /// Immediately handles an event, bypassing other enqueued events, using the regular subscribers, from the current thread.
        /// Concurrent calls are forced to wait, until the first call finishes.
        /// Suspends regular event handling for the duration (though regular event handling already in progress will keep running).
        /// Events need to implement <see cref="ICriticalEvent"/>.
        /// <see cref="IEventQueue.EventHandling"/> is ignored.
        /// May fail if <see cref="EventQueueBase.BeforeAdding"/> returned <see cref="EventQueueBase.HandlingOption.UnableToHandle"/>.
        /// </summary>
        /// <param name="criticalEvent">The critical event to handle.</param>
        /// <param name="file">The source file of the caller.</param>
        /// <param name="member">The method or property name of the caller to this method.</param>
        /// <param name="line">The line number in <paramref name="file"/>.</param>
        /// <returns><c>true</c> if an event was handled successfully; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// The main use case for this is when you need to have an event handled, before control leaves your method
        /// (e.g. when the application is about to terminate, and your code may never get control again).
        /// Event handlers MUST be quick, since the app may need to terminate in the next 1-2 seconds (or less).
        /// Non-critical code (e.g. GUI and other state changes) should always be triggered by a separate, non-critical event.
        /// It is possible for regular event handlers to run, while this happens!
        /// (since suspension does not pause/abort event handling already in progress)
        /// </remarks>
        public bool HandleCritical(
            EventBase criticalEvent,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
        {
            if( !(criticalEvent is ICriticalEvent) )
                throw new ArgumentException($"{criticalEvent.GetType().FullName} is not a critical event!");

            // suspend regular event handling (this does not stop event handling currently underway)
            this.EventHandling.Suspend();

            try
            {
                // handle critical event, right now
                lock( this.criticalEventHandlingLock ) // we do this here, so that concurrent calls all "suspend" before waiting for their turn (so that regular event handling can not "flicker" on)
                {
                    // observes RaiseUnhandledExceptionEvents feature,
                    // schedules them as regular events
                    // (we don't want to take away time from critical tasks).
                    return this.regularEventQueue.TryHandleEventIgnoringSuspension(criticalEvent);
                }
            }
            finally
            {
                // resume regular event handling
                this.EventHandling.Resume();
            }
        }

        #endregion
    }
}
