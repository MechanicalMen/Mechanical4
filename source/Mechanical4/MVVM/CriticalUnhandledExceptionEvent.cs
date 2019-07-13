using System;
using Mechanical4.EventQueue;

namespace Mechanical4.MVVM
{
    /// <summary>
    /// The critical event version of <see cref="UnhandledExceptionEvent"/>.
    /// </summary>
    public class CriticalUnhandledExceptionEvent : UnhandledExceptionEvent, ICriticalEvent
    {
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
        /// <param name="type">The exception type.</param>
        /// <param name="message">The exception message.</param>
        /// <param name="asString">The serialized exception string.</param>
        public CriticalUnhandledExceptionEvent( string type, string message, string asString )
            : base(type, message, asString)
        {
        }
    }
}
