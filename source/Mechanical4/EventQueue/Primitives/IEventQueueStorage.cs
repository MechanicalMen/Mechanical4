namespace Mechanical4.EventQueue.Primitives
{
    /// <summary>
    /// Thread-safe, temporary event storage.
    /// The order of stored events is undetermined
    /// (so you could use this to implement a priority system).
    /// </summary>
    public interface IEventQueueStorage
    {
        /// <summary>
        /// Tries to add the specified event to the storage.
        /// The storage may refuse to store certain events
        /// (e.g. multiple <see cref="ShuttingDownEvent"/>s).
        /// </summary>
        /// <param name="evnt">The event to enqueue.</param>
        /// <returns><c>true</c> if the event was added to the storage; otherwise, <c>false</c>.</returns>
        bool TryPush( EventBase evnt );

        /// <summary>
        /// Tries to remove an event from the storage.
        /// Returns <c>null</c> if the storage was empty.
        /// </summary>
        /// <returns>The event removed, or <c>null</c>.</returns>
        EventBase TryPop();

        /// <summary>
        /// Determines whether the specified event reference is stored.
        /// </summary>
        /// <param name="evnt">The event to search for within the storage.</param>
        /// <returns><c>true</c> if the specified event reference was found; otherwise, <c>false</c>.</returns>
        bool Contains( EventBase evnt );

        /// <summary>
        /// Gets a value indicating whether this storage is empty.
        /// </summary>
        bool IsEmpty { get; }
    }
}
