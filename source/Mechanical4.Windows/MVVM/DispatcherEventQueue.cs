﻿using System;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using Mechanical4.EventQueue;
using Mechanical4.EventQueue.Primitives;
using Mechanical4.Misc;

namespace Mechanical4.MVVM
{
    /// <summary>
    /// Uses a <see cref="Dispatcher"/> for event handling.
    /// This would allow the running of event handlers from the UI thread,
    /// but may result in freezing the UI, if handlers take too long.
    /// </summary>
    public class DispatcherEventQueue : IEventQueue
    {
        //// NOTE: This is an experiment, and it's usefulness is questionable.
        ////       The use case would be event handlers that must be run from the UI thread.
        ////       This would complement a non-UI bound main event queue.

        //// NOTE: We may schedule more than one event handler on the dispatcher,
        ////       but that is fine, since they are executed one by one.

        #region Private Fields

        private readonly Dispatcher dispatcher;
        private readonly DispatcherPriority dispatcherPriority;
        private readonly ThreadSafeBoolean isEventHandlerScheduled;
        private readonly ManualEventQueue manualQueue;
        private readonly Delegate scheduledDelegate;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DispatcherEventQueue"/> class.
        /// </summary>
        /// <param name="uiDispatcher">The <see cref="Dispatcher"/> to use for event handling.</param>
        /// <param name="priority">The <see cref="DispatcherPriority"/> to use. Background priority is the default, to keep the UI snappy.</param>
        public DispatcherEventQueue( Dispatcher uiDispatcher, DispatcherPriority priority = DispatcherPriority.Background )
        {
            if( !Enum.IsDefined(typeof(DispatcherPriority), priority) )
                throw new ArgumentException("Invalid priority!");

            this.dispatcher = uiDispatcher ?? throw Exc.Null(nameof(uiDispatcher));
            this.dispatcherPriority = priority;
            this.isEventHandlerScheduled = new ThreadSafeBoolean(false);
            this.manualQueue = new ManualEventQueue();
            this.scheduledDelegate = new Action(this.OnScheduled);
        }

        #endregion

        #region Private Methods

        private void ScheduleEventHandling()
        {
            if( this.EventHandling.IsSuspended )
                return;

            this.isEventHandlerScheduled.SetIfEquals(true, comparand: false, oldValue: out bool wasScheduling);
            if( !wasScheduling )
                this.dispatcher.BeginInvoke(this.scheduledDelegate, this.dispatcherPriority, args: null);
        }

        private void OnScheduled()
        {
            //// NOTE: Handling all available events would be more efficient,
            ////       but that has the risk of blocking the UI thread for too long.

            this.isEventHandlerScheduled.Set(false); // do this first, so that there is no issue, if an event is enqueued after the last event was handled, but before this method exits
            if( this.manualQueue.HandleNext() )
                this.ScheduleEventHandling();
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
            bool result = this.manualQueue.Enqueue(evnt, file, member, line);
            if( result )
                this.ScheduleEventHandling();
            return result;
        }

        /// <summary>
        /// Gets the collection of event handlers.
        /// </summary>
        public EventSubscriberCollection Subscribers => this.manualQueue.Subscribers;

        /// <summary>
        /// Gets the object managing whether the handling of events already in the queue can be started.
        /// Does not affect event handling already in progress.
        /// Does not affect the addition of events to the queue (i.e. <see cref="Enqueue"/>).
        /// </summary>
        public EventQueueFeature EventHandling => this.manualQueue.EventHandling;

        /// <summary>
        /// Gets the object managing whether events are silently discarded, instead of being added to the queue.
        /// This affects neither events already in the queue, nor their handling.
        /// </summary>
        public EventQueueFeature EventAdding => this.manualQueue.EventAdding;

        /// <summary>
        /// Gets the object managing whether exceptions thrown by event handlers
        /// are wrapped and raised as <see cref="UnhandledExceptionEvent"/>.
        /// <see cref="EventAdding"/> must be enabled for this to work.
        /// </summary>
        public EventQueueFeature RaiseUnhandledEvents => this.manualQueue.RaiseUnhandledEvents;

        #endregion
    }
}
