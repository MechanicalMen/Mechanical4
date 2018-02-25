using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Mechanical4.Core;
using Mechanical4.EventQueue.Events;

namespace Mechanical4.EventQueue
{
    /// <summary>
    /// Wraps an <see cref="IEventQueue"/>, and uses it to handle non-critical events.
    /// Critical events however are handled immediately, from within <see cref="IEventQueue.Enqueue"/>, synchronously.
    /// In rare cases, non-critical and critical event handlers may be executed in parallel.
    /// </summary>
    public class MainEventQueue : IEventQueue
    {
        #region Private Fields

        private readonly IEventQueue nonCriticalQueue;
        private readonly List<EventBase> criticalEvents;
        private readonly List<Exception> eventHandlerExceptions;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MainEventQueue"/> class.
        /// </summary>
        /// <param name="wrappee">The <see cref="IEventQueue"/> to wrap. Does NOT take ownership of it.</param>
        public MainEventQueue( IEventQueue wrappee )
        {
            this.nonCriticalQueue = wrappee ?? throw Exc.Null(nameof(wrappee));
            this.criticalEvents = new List<EventBase>();
            this.eventHandlerExceptions = new List<Exception>();
        }

        #endregion

        #region IEventQueue

        /// <summary>
        /// Enqueues an event, to be handled by subscribers sometime later.
        /// There is no guarantee that the event will end up being handled
        /// (e.g. closed queues can not enqueue, and the application
        /// may be terminated beforehand).
        /// Suspended event queues can still enqueue events (see <see cref="IEventQueue.IsSuspended"/>).
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

            if( !(evnt is ICriticalEvent) )
            {
                // non-critical event: enqueue as usual
                this.nonCriticalQueue.Enqueue(evnt);
            }
            else
            {
                //// critical event

                // save event for handling
                ManualEventQueue.SetMetaData(evnt, file, member, line);
                this.criticalEvents.Add(evnt);

                // return, if a critical event is already being handled
                // (we must have been invoked from within a critical event handler)
                if( this.criticalEvents.Count > 1 )
                    return;

                // this is the first critical event: suspend handling of non-critical events
                this.nonCriticalQueue.IsSuspended = true;

                // handle all critical events
                while( this.criticalEvents.Count != 0 )
                {
                    var e = this.criticalEvents[0];
                    this.criticalEvents.RemoveAt(0);

                    var exception = ManualEventQueue.HandleEvent(e, this.Subscribers, this.eventHandlerExceptions);
                    if( exception.NotNullReference() )
                    {
                        this.criticalEvents.Add(
                            new CriticalUnhandledExceptionEvent(
                                exception));
                    }
                }

                // resume handling of non-critical events
                this.nonCriticalQueue.IsSuspended = false;
            }
        }

        /// <summary>
        /// Gets the collection of event handlers.
        /// </summary>
        public EventSubscriberCollection Subscribers => this.nonCriticalQueue.Subscribers;

        /// <summary>
        /// Gets or sets a value indicating whether the handling of enqueued events is currently allowed.
        /// Suspension does not apply to event handling already in progress.
        /// </summary>
        bool IEventQueue.IsSuspended
        {
            get => throw new NotSupportedException().StoreFileLine();
            set => throw new NotSupportedException().StoreFileLine();
        }

        #endregion
    }
}
