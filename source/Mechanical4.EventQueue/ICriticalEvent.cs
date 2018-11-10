namespace Mechanical4.EventQueue
{
    /// <summary>
    /// Critical events MUST be handled immediately and quickly, since
    /// the application may need to terminate in the next 1-2 seconds (or less).
    /// Currently only the <see cref="MainEventQueue"/> handles them correctly.
    /// Listeners MUST be thread-safe, since they can be invoked from any thread.
    /// GUI and state changes should be done using a second, non-critical event.
    /// </summary>
    public interface ICriticalEvent
    {
    }
}
