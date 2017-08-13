using System;

namespace Mechanical4.EventQueue.Serialization
{
    /// <summary>
    /// Represents a data storage resource, that events can be written to.
    /// </summary>
    public interface IEventStreamWriter : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether this instance expects to write size optimized content.
        /// </summary>
        bool IsCompactFormat { get; }

        /// <summary>
        /// Starts writing a new event.
        /// </summary>
        /// <returns>The <see cref="IEventWriter"/> to use.</returns>
        IEventWriter BeginNewEvent();

        /// <summary>
        /// Stops writing of the last event.
        /// </summary>
        void EndLastEvent();
    }
}
