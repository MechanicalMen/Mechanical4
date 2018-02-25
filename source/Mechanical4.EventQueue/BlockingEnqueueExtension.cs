using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Mechanical4.Core;

namespace Mechanical4.EventQueue
{
    /// <summary>
    /// Blocking versions of <see cref="IEventQueue.Enqueue"/>.
    /// </summary>
    public static class BlockingEnqueueExtension
    {
        #region WaitingEvent

        private class WaitingEvent : EventBase
        {
            private static int lastIndex = 0;

            public WaitingEvent()
            {
                this.Index = Interlocked.Increment(ref lastIndex);
            }

            public int Index { get; }
        }

        #endregion

        #region WaitingListener

        private class WaitingListener : IEventHandler<WaitingEvent>
        {
            private readonly TaskCompletionSource<object> tsc;
            private readonly IEventQueue queue;
            private readonly int index;

            public WaitingListener( IEventQueue eventQueue, int eventIndex )
            {
                this.tsc = new TaskCompletionSource<object>();
                this.queue = eventQueue ?? throw Exc.Null(nameof(eventQueue));
                this.index = eventIndex;
            }

            public void Handle( WaitingEvent evnt )
            {
                if( evnt.Index == this.index )
                {
                    this.tsc.TrySetResult(null);
                    this.queue.Subscribers.Remove(this);
                }
            }

            public Task Task => this.tsc.Task;
        }

        #endregion

        #region EnqueueAndWaitAsync

        /// <summary>
        /// Enqueues an event, to be handled by subscribers sometime later.
        /// There is no guarantee that the event will end up being handled
        /// (e.g. closed queues can not enqueue, and the application
        /// may be terminated beforehand).
        /// Suspended event queues can still enqueue events (see <see cref="IEventQueue.IsSuspended"/>).
        /// </summary>
        /// <param name="eventQueue">The event queue to add the event to.</param>
        /// <param name="evnt">The event to enqueue.</param>
        /// <param name="file">The source file of the caller.</param>
        /// <param name="member">The method or property name of the caller to this method.</param>
        /// <param name="line">The line number in <paramref name="file"/>.</param>
        /// <returns>An object representing the asynchronous operation.</returns>
        public static Task EnqueueAndWaitAsync(
            this IEventQueue eventQueue,
            EventBase evnt,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
        {
            var waitingEvent = new WaitingEvent();
            var waitingListener = new WaitingListener(eventQueue, waitingEvent.Index);
            eventQueue.Subscribers.Add(waitingListener, useWeakRef: false);

            eventQueue.Enqueue(evnt, file, member, line);
            eventQueue.Enqueue(waitingEvent, file, member, line);

            return waitingListener.Task;
        }

        #endregion
    }
}
