using Mechanical4.EventQueue.Serialization;
using Mechanical4.Core;

namespace Mechanical4.EventQueue.Events
{
    /// <summary>
    /// This kind of event should be used very rarely, since usually different
    /// kinds of events should be represented by different subclasses of <see cref="EventBase"/>.
    /// But for quick and dirty applications, debugging, and one-off events, this may be more economical.
    /// </summary>
    public class NamedEvent : SerializableEventBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NamedEvent"/> class.
        /// </summary>
        /// <param name="name">The string specifying the type of the event.</param>
        public NamedEvent( string name )
        {
            this.Name = name;
        }

        /// <summary>
        /// Gets the string specifying the type of the event.
        /// </summary>
        public string Name { get; }

        #region Serialization

        /// <summary>
        /// Initializes a new instance of the <see cref="NamedEvent"/> class.
        /// </summary>
        /// <param name="reader">The <see cref="IEventReader"/> to deserialize from.</param>
        public NamedEvent( IEventReader reader )
        {
            if( reader.NullReference() )
                throw Exc.Null(nameof(reader));

            this.Name = reader.ReadString();
        }

        /// <summary>
        /// Serializes the components of the event, other than <see cref="SerializableEventBase.EventEnqueueTime"/> and <see cref="EventBase.EventEnqueuePos"/>.
        /// </summary>
        /// <param name="writer">The <see cref="IEventWriter"/> to use.</param>
        public override void Serialize( IEventWriter writer )
        {
            if( writer.NullReference() )
                throw Exc.Null(nameof(writer));

            writer.Write(this.Name);
        }

        #endregion
    }
}
