namespace Mechanical4.EventQueue.Events
{
    /// <summary>
    /// This kind of event should be used very rarely, since usually different
    /// kinds of events should be represented by different subclasses of <see cref="EventBase"/>.
    /// But for quick and dirty applications, debugging, and one-off events, this may be more economical.
    /// </summary>
    public class NamedEvent : EventBase
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
    }
}
