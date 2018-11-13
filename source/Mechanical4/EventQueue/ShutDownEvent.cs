namespace Mechanical4.EventQueue
{
    /// <summary>
    /// Indicates that the event queue this event originates from, has shut down.
    /// After this event has been handled, the subscribers are automatically removed,
    /// and no more subscriptions can be registered.
    /// </summary>
    internal class ShutDownEvent : EventBase
    {
        //// NOTE: this event is deliberately not public, because we do not want users
        ////       to register for this instead of ShuttingDownEvent.
        ////       We can not avoid it however, since we have need of it.

        //// NOTE: if the accessibility is changed later, we should disallow enqueueing more than one.

        /// <summary>
        /// Initializes a new instance of the <see cref="ShutDownEvent"/> class.
        /// </summary>
        internal ShutDownEvent()
        {
        }
    }
}
