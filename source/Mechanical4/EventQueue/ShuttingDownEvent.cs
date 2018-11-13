namespace Mechanical4.EventQueue
{
    /// <summary>
    /// Indicates that the event queue this event originates from,
    /// is irrevocably in the process of shutting down.
    /// After this event has been handled, adding new events will have no effect.
    /// After all events have been handled, the subscribers are automatically removed,
    /// and no more subscriptions can be registered.
    /// </summary>
    public class ShuttingDownEvent : EventBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ShuttingDownEvent"/> class.
        /// </summary>
        public ShuttingDownEvent()
        {
        }
    }
}
