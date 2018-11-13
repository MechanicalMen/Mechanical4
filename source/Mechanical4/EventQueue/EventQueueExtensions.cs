using System;
using System.Runtime.CompilerServices;

namespace Mechanical4.EventQueue
{
    /// <summary>
    /// Extension methods for the <see cref="Mechanical4.EventQueue"/> namespace.
    /// </summary>
    public static class EventQueueExtensions
    {
        /// <summary>
        /// Enqueues a <see cref="ShutdownRequestEvent"/>.
        /// </summary>
        /// <param name="eventQueue">The <see cref="IEventQueue"/> to use.</param>
        /// <param name="onRequestHandled">An optional delegate invoked sometime after the event finished being handled. It may not get called, if the event queue is suspended or being shut down. Check the return value to know whether you can reasonably expect it.</param>
        /// <param name="file">The source file of the caller.</param>
        /// <param name="member">The method or property name of the caller to this method.</param>
        /// <param name="line">The line number in <paramref name="file"/>.</param>
        /// <returns><c>true</c> if the event was enqueued successfully and you can expect a callback once it's done being handled; otherwise, <c>false</c>.</returns>
        public static bool RequestShutdown(
            this IEventQueue eventQueue,
            Action<ShutdownRequestEvent> onRequestHandled = null,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
        {
            Action<EventBase> action = null;
            if( onRequestHandled.NotNullReference() )
                action = e => onRequestHandled((ShutdownRequestEvent)e);

            return eventQueue.Enqueue(
                new ShutdownRequestEvent(),
                action,
                file,
                member,
                line);
        }

        /// <summary>
        /// Enqueues a <see cref="ShuttingDownEvent"/>.
        /// </summary>
        /// <param name="eventQueue">The <see cref="IEventQueue"/> to use.</param>
        /// <param name="file">The source file of the caller.</param>
        /// <param name="member">The method or property name of the caller to this method.</param>
        /// <param name="line">The line number in <paramref name="file"/>.</param>
        /// <returns><c>true</c> if the event was enqueued successfully; otherwise, <c>false</c>.</returns>
        public static bool BeginShutdown(
            this IEventQueue eventQueue,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
        {
            return eventQueue.Enqueue(new ShuttingDownEvent(), file, member, line);
        }
    }
}
