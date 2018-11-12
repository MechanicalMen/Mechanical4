using System.Runtime.CompilerServices;
using Mechanical4.EventQueue.Events;

namespace Mechanical4.EventQueue
{
    /// <summary>
    /// Extension methods for the <see cref="Mechanical4.EventQueue"/> namespace.
    /// </summary>
    public static class EventQueueExtensions
    {
        /// <summary>
        /// Enqueues an <see cref="EventQueueClosingEvent"/>.
        /// </summary>
        /// <param name="eventQueue">The <see cref="IEventQueue"/> to use.</param>
        /// <param name="file">The source file of the caller.</param>
        /// <param name="member">The method or property name of the caller to this method.</param>
        /// <param name="line">The line number in <paramref name="file"/>.</param>
        /// <returns><c>true</c> if the event was enqueued successfully; otherwise, <c>false</c>.</returns>
        public static bool BeginClose(
            this IEventQueue eventQueue,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
        {
            return eventQueue.Enqueue(
                new EventQueueClosingEvent(),
                file,
                member,
                line);
        }
    }
}
