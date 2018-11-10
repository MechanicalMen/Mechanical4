using System;

namespace Mechanical4.EventQueue.Events
{
    /// <summary>
    /// Use <see cref="UnhandledExceptionEvent"/>, unless you absolutely need this.
    /// Mainly for when the application is about to terminate due to an exception.
    /// Critical events need to be handled very quickly (see <see cref="ICriticalEvent"/>),
    /// and can only be handled by special event queues (e.g. <see cref="MainEventQueue"/>).
    /// </summary>
    public class CriticalUnhandledExceptionEvent : UnhandledExceptionEvent, ICriticalEvent
    {
        //// NOTE: UnhandledExceptionEvent needs to remain non-critical, since most event queues can only handle them.

        /// <summary>
        /// Initializes a new instance of the <see cref="CriticalUnhandledExceptionEvent"/> class.
        /// </summary>
        /// <param name="exception">The exception to report.</param>
        public CriticalUnhandledExceptionEvent( Exception exception )
            : base(exception)
        {
        }
    }
}
