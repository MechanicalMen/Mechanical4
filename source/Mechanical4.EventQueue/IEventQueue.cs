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
        /// (e.g. closed queues can not enqueue, and the application
        /// may be terminated beforehand).
        /// Suspended event queues can still enqueue events (see <see cref="IsSuspended"/>).
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
        /// Gets or sets a value indicating whether the handling of enqueued events is currently allowed.
        /// Suspension does not apply to event handling already in progress.
        /// </summary>
        bool IsSuspended { get; set; }
    }
}
