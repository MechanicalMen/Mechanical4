namespace Mechanical4.EventQueue
{
    /// <summary>
    /// Handles events of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of event to handle.</typeparam>
    public interface IEventHandler<T>
        where T : EventBase
    {
        /// <summary>
        /// Handles the specified event.
        /// </summary>
        /// <param name="evnt">The event to handle.</param>
        void Handle( T evnt );
    }
}
