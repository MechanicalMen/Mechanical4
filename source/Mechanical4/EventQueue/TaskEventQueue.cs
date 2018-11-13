using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Mechanical4.EventQueue
{
    /// <summary>
    /// Uses a long running task to execute event handlers as events become available.
    /// </summary>
    public class TaskEventQueue : IEventQueue
    {
        #region Private Fields

        private readonly ManualEventQueue manualQueue;
        private readonly ManualResetEventSlim newEventWaitHandle;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskEventQueue"/> class.
        /// </summary>
        public TaskEventQueue()
        {
            this.manualQueue = new ManualEventQueue();
            this.newEventWaitHandle = new ManualResetEventSlim(initialState: false); // nonsignaled, blocks
            this.EventHandling = new FeatureSuspender(
                onSuspended: this.SuspendTask,
                onResumed: this.ResumeTask);
            this.Task = Task.Factory.StartNew(this.Work, TaskCreationOptions.LongRunning);
        }

        #endregion

        #region Private Methods

        private void Work()
        {
            while( true )
            {
                // wait for the next Enqueue call
                this.newEventWaitHandle.Wait(); // wait indefinitely for events to be added
                this.SuspendTask(); // wait again after we handled all available events

                // handle all that we can
                while( this.manualQueue.HandleNext() ) ;

                // finish if shut down
                if( this.manualQueue.IsShutDown )
                    break;
            }
        }

        private void SuspendTask() => this.newEventWaitHandle.Reset(); // nonsignaled, blocks
        private void ResumeTask() => this.newEventWaitHandle.Set(); // signaled, does not block

        #endregion

        #region IEventQueue

        /// <summary>
        /// Enqueues an event, to be handled by subscribers sometime later.
        /// There is no guarantee that the event will end up being handled
        /// (e.g. suspended or shut down queues silently ignore events,
        /// or the application may be terminated beforehand).
        /// </summary>
        /// <param name="evnt">The event to enqueue.</param>
        /// <param name="file">The source file of the caller.</param>
        /// <param name="member">The method or property name of the caller to this method.</param>
        /// <param name="line">The line number in <paramref name="file"/>.</param>
        /// <returns><c>true</c> if the event was enqueued successfully; otherwise, <c>false</c>.</returns>
        public bool Enqueue(
            EventBase evnt,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
        {
            var eventAdded = this.manualQueue.Enqueue(evnt, file, member, line);

            if( eventAdded
             && this.EventHandling.IsEnabled )
                this.ResumeTask();

            return eventAdded;
        }

        /// <summary>
        /// Gets the collection of event handlers.
        /// </summary>
        public EventSubscriberCollection Subscribers => this.manualQueue.Subscribers;

        /// <summary>
        /// Gets the object managing whether the handling of events already in the queue can be started.
        /// Does not affect event handling already in progress.
        /// Does not affect the addition of events to the queue (i.e. <see cref="Enqueue"/>).
        /// </summary>
        public FeatureSuspender EventHandling { get; }

        /// <summary>
        /// Gets the object managing whether events are silently discarded, instead of being added to the queue.
        /// This affects neither events already in the queue, nor their handling.
        /// </summary>
        public FeatureSuspender EventAdding => this.manualQueue.EventAdding;

        /// <summary>
        /// Gets the object managing whether exceptions thrown by event handlers
        /// are wrapped and raised as <see cref="UnhandledExceptionEvent"/>.
        /// <see cref="EventAdding"/> must be enabled for this to work.
        /// </summary>
        public FeatureSuspender RaiseUnhandledEvents => this.manualQueue.RaiseUnhandledEvents;

        #endregion

        #region Public Members

        /// <summary>
        /// Gets the task doing the event processing.
        /// </summary>
        public Task Task { get; }

        #endregion
    }
}
