namespace Mechanical4.EventQueue
{
    /// <summary>
    /// The base class all events must inherit.
    /// </summary>
    public abstract class EventBase
    {
        /// <summary>
        /// Gets the position in the source file, from where this event was enqueued the last time;
        /// or <c>null</c>, if the event was never enqueued.
        /// The value is used by the queue, for throwing more informative exceptions
        /// (since the event source is not part of the stack tree of an event handler's exception).
        /// </summary>
        public string EventEnqueuePos { get; internal set; }
    }
}
