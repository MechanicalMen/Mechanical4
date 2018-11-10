namespace Mechanical4.EventQueue.Events
{
    /// <summary>
    /// After this event has been handled, adding new events will have no effect.
    /// After all events have been handled, the subscribers are automatically removed,
    /// and no more subscriptions can be registered.
    /// </summary>
    public class EventQueueClosingEvent : EventBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventQueueClosingEvent"/> class.
        /// </summary>
        public EventQueueClosingEvent()
        {
        }
    }
}
