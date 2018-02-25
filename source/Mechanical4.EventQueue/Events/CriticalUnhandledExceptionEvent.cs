using System;
using Mechanical4.EventQueue.Serialization;

namespace Mechanical4.EventQueue.Events
{
    /// <summary>
    /// A critical event version of <see cref="UnhandledExceptionEvent"/>.
    /// Use with an applicable event queue (e.g. <see cref="MainEventQueue"/>),
    /// when the exception needs to be handled immediately and quickly
    /// (e.g. the application is about to terminate due to this exception).
    /// Use <see cref="UnhandledExceptionEvent"/>, unless you absolutely need this.
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

        /// <summary>
        /// Initializes a new instance of the <see cref="CriticalUnhandledExceptionEvent"/> class.
        /// </summary>
        /// <param name="reader">The <see cref="IEventReader"/> to deserialize from.</param>
        public CriticalUnhandledExceptionEvent( IEventReader reader )
            : base(reader)
        {
        }
    }
}
