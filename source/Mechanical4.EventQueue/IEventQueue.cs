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
        /// </summary>
        /// <param name="evnt">The event to enqueue.</param>
        /// <param name="critical"><c>true</c> if the event needs to be handled before other non-critical events; otherwise, <c>false</c>.</param>
        /// <param name="file">The source file of the caller.</param>
        /// <param name="member">The method or property name of the caller to this method.</param>
        /// <param name="line">The line number in <paramref name="file"/>.</param>
        void Enqueue(
            EventBase evnt,
            bool critical = false,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 );

        /// <summary>
        /// Gets the collection of event handlers.
        /// </summary>
        EventSubscriberCollection Subscribers { get; }
    }
}
