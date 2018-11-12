using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Mechanical4.EventQueue
{
    /// <summary>
    /// Blocking versions of <see cref="IEventQueue.Enqueue"/>.
    /// </summary>
    public static class CallbackEnqueueExtension
    {
        #region FinishedEvent

        private class FinishedEvent : EventBase
        {
            private static int lastIndex = 0;

            public FinishedEvent( EventBase originalEvent )
            {
                this.Index = Interlocked.Increment(ref lastIndex);
                this.OriginalEvent = originalEvent ?? throw Exc.Null(nameof(originalEvent));
            }

            public int Index { get; }
            public EventBase OriginalEvent { get; }
        }

        #endregion

        #region FinishedEventHandler

        private class FinishedEventHandler : IEventHandler<FinishedEvent>
        {
            private readonly Action<EventBase> action;
            private readonly IEventQueue queue;
            private readonly int index;

            public FinishedEventHandler( IEventQueue eventQueue, int eventIndex, Action<EventBase> onHandled )
            {
                this.queue = eventQueue ?? throw Exc.Null(nameof(eventQueue));
                this.index = eventIndex;
                this.action = onHandled ?? throw Exc.Null(nameof(onHandled));
            }

            public void Handle( FinishedEvent evnt )
            {
                if( evnt.Index == this.index )
                {
                    this.queue.Subscribers.Remove(this);
                    this.action(evnt.OriginalEvent);
                }
            }
        }

        #endregion

        #region Enqueue

        /// <summary>
        /// Enqueues an event, to be handled by subscribers sometime later.
        /// There is no guarantee that the event will end up being handled
        /// (e.g. suspended or closed queues silently ignore events,
        /// or the application may be terminated beforehand).
        /// </summary>
        /// <param name="eventQueue">The event queue to add the event to.</param>
        /// <param name="evnt">The event to enqueue.</param>
        /// <param name="onHandled">An optional delegate to invoke sometime after the event finished being handled. It may not get called, if the event queue is suspended or being closed. Check the return value to know whether you can reasonably expect it.</param>
        /// <param name="file">The source file of the caller.</param>
        /// <param name="member">The method or property name of the caller to this method.</param>
        /// <param name="line">The line number in <paramref name="file"/>.</param>
        /// <returns><c>true</c> if the event was enqueued successfully and you can expect a callback once it's done being handled; otherwise, <c>false</c>.</returns>
        public static bool Enqueue(
            this IEventQueue eventQueue,
            EventBase evnt,
            Action<EventBase> onHandled,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
        {
            if( onHandled.NullReference() )
                return eventQueue.Enqueue(evnt, file, member, line);

            var finishedEvent = new FinishedEvent(evnt);
            var finishedListener = new FinishedEventHandler(eventQueue, finishedEvent.Index, onHandled);
            eventQueue.Subscribers.Add(finishedListener, weakRef: false);

            var eventsAdded = eventQueue.Enqueue(evnt, file, member, line)
                && eventQueue.Enqueue(finishedEvent, file, member, line); // doesn't even get called if the first one fails, which is what we want.

            if( !eventsAdded )
                eventQueue.Subscribers.Remove(finishedListener);

            return eventsAdded;
        }

        #endregion
    }
}
