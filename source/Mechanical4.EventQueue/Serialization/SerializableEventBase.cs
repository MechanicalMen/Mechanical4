using System;

namespace Mechanical4.EventQueue.Serialization
{
    /// <summary>
    /// The base class of all serializable events.
    /// </summary>
    public abstract class SerializableEventBase : EventBase
    {
        /// <summary>
        /// Gets a value indicating when the event was last enqueued.
        /// The value is undetermined beforehand.
        /// </summary>
        public DateTime EventEnqueueTime { get; internal set; } = DateTime.MinValue;

        /// <summary>
        /// Serializes the components of the event, other than <see cref="SerializableEventBase.EventEnqueueTime"/> and <see cref="EventBase.EventEnqueuePos"/>.
        /// </summary>
        /// <param name="writer">The <see cref="IEventWriter"/> to use.</param>
        public abstract void Serialize( IEventWriter writer );
    }
}
