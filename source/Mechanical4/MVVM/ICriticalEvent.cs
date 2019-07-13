namespace Mechanical4.MVVM
{
    /// <summary>
    /// Indicates that the instances should be handled as a "critical" event.
    /// Note that currently, this can only happen through <see cref="MainEventQueue.HandleCritical"/>,
    /// which is a blocking call, handling events synchronously.
    /// Such events are typically used in time-critical scenarios, like when the process is
    /// about to exit. This means that subscribers must handle them as quickly as possible.
    /// </summary>
    public interface ICriticalEvent
    {
    }
}
