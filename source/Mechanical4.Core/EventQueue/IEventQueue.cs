using System.Runtime.CompilerServices;

namespace Mechanical4.EventQueue
{
    /// <summary>
    /// Transmits events to event handlers.
    /// </summary>
    public interface IEventQueue
    {
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
        void Enqueue(
            EventBase evnt,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 );

        /// <summary>
        /// Gets the collection of event handlers.
        /// </summary>
        EventSubscriberCollection Subscribers { get; }

        /// <summary>
        /// Gets the object managing whether the handling of events already in the queue can be started.
        /// Does not affect event handling already in progress.
        /// Does not affect the addition of events to the queue (i.e. <see cref="Enqueue"/>).
        /// </summary>
        FeatureSuspender EventHandling { get; }

        /// <summary>
        /// Gets the object managing whether events are silently discarded, instead of being added to the queue.
        /// This affects neither events already in the queue, nor their handling.
        /// </summary>
        FeatureSuspender EventAdding { get; }
    }
}
