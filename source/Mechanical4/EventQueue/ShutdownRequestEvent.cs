namespace Mechanical4.EventQueue
{
    /// <summary>
    /// A cancellable event that may be optionally raised to determine, whether the event queue should be shut down.
    /// Event handlers may wish to block to determine whether changes should be saved or discarded.
    /// Unless the event is cancelled, a <see cref="ShuttingDownEvent"/> is automatically
    /// enqueued once the event has finished handling.
    /// </summary>
    public class ShutdownRequestEvent : EventBase
    {
        /// <summary>
        /// Initializes a new instance of the <seealso cref="ShutdownRequestEvent"/> class.
        /// </summary>
        public ShutdownRequestEvent()
        {
            // by default we allow the request to go through
            // (based on WinForms (Form.FormClosing) and WPF (Window.Closing) behavior)
            this.Cancel = false;
        }

        /// <summary>
        /// Gets or sets a value determining whether the event queue should be shut down.
        /// <c>true</c> indicates that the queue should remain as it is (for now);
        /// <c>false</c> indicates that it should start to gracefully stop.
        /// </summary>
        public bool Cancel { get; set; }
    }
}
