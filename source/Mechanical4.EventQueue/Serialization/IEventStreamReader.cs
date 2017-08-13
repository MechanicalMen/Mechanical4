using System;

namespace Mechanical4.EventQueue.Serialization
{
    /// <summary>
    /// Represents a data storage resource, that events can be read from.
    /// </summary>
    public interface IEventStreamReader : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether this instance expects to read size optimized content.
        /// </summary>
        bool IsCompactFormat { get; }

        /// <summary>
        /// Tries to read the next event from the stream.
        /// </summary>
        /// <returns>The <see cref="IEventReader"/> of the next event; or <c>null</c> if there are no more events</returns>
        IEventReader TryRead();
    }
}
