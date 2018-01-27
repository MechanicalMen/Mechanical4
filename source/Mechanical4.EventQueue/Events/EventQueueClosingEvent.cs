using Mechanical4.EventQueue.Serialization;
using Mechanical4.Core;

namespace Mechanical4.EventQueue.Events
{
    /// <summary>
    /// After this event has been handled, adding new events will have no effect.
    /// After all events have been handled, the subscribers are automatically removed,
    /// and no more subscriptions can be registered.
    /// </summary>
    public class EventQueueClosingEvent : SerializableEventBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventQueueClosingEvent"/> class.
        /// </summary>
        /// <param name="critical">Indicates whether this is a critical closing event (see <see cref="IsCritical"/>).</param>
        public EventQueueClosingEvent( bool critical )
        {
            this.IsCritical = critical;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventQueueClosingEvent"/> class.
        /// </summary>
        /// <param name="reader">The <see cref="IEventReader"/> to deserialize from.</param>
        public EventQueueClosingEvent( IEventReader reader )
        {
            if( reader.NullReference() )
                throw Exc.Null(nameof(reader));

            this.IsCritical = reader.ReadBoolean();
        }

        /// <summary>
        /// Serializes the components of the event, other than <see cref="SerializableEventBase.EventEnqueueTime"/> and <see cref="EventBase.EventEnqueuePos"/>.
        /// </summary>
        /// <param name="writer">The <see cref="IEventWriter"/> to use.</param>
        public override void Serialize( IEventWriter writer )
        {
            if( writer.NullReference() )
                throw Exc.Null(nameof(writer));

            writer.Write(this.IsCritical);
        }

        /// <summary>
        /// Gets a value indicating whether this is a critical closing event.
        /// Critical closing events indicate that the application is forced to terminate,
        /// and may only have 1-2 seconds (or less) of execution time left.
        /// Listeners should only worry about preserving data, and finishing ASAP.
        /// Such events also force their queue to only handle critical events.
        /// </summary>
        public bool IsCritical { get; }
    }
}
