using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Mechanical4.EventQueue.Events;

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
        private readonly Task task;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskEventQueue"/> class.
        /// </summary>
        public TaskEventQueue()
        {
            this.manualQueue = new ManualEventQueue();
            this.newEventWaitHandle = new ManualResetEventSlim(initialState: false); // nonsignaled, blocks
            this.task = Task.Factory.StartNew(this.Work, TaskCreationOptions.LongRunning);
        }

        #endregion

        #region Private Methods

        private void Work()
        {
            while( true )
            {
                // wait for the next Enqueue call
                this.newEventWaitHandle.Wait(); // wait indefinitely
                this.newEventWaitHandle.Reset(); // nonsignaled, blocks

                // handle all that we can
                while( this.manualQueue.HandleNext() ) ;

                // finish if closed
                if( this.manualQueue.IsClosed )
                    break;
            }
        }

        #endregion

        #region IEventQueue

        /// <summary>
        /// Enqueues an event, to be handled by subscribers sometime later.
        /// </summary>
        /// <param name="evnt">The event to enqueue.</param>
        /// <param name="critical"><c>true</c> if the event needs to be handled before other non-critical events; otherwise, <c>false</c>.</param>
        /// <param name="file">The source file of the caller.</param>
        /// <param name="member">The method or property name of the caller to this method.</param>
        /// <param name="line">The line number in <paramref name="file"/>.</param>
        public void Enqueue(
            EventBase evnt,
            bool critical = false,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
        {
            this.manualQueue.Enqueue(evnt, critical, file, member, line);
            this.newEventWaitHandle.Set(); // signaled, does not block
        }

        /// <summary>
        /// Gets the collection of event handlers.
        /// </summary>
        public EventSubscriberCollection Subscribers => this.manualQueue.Subscribers;

        #endregion

        #region Public Members

        /// <summary>
        /// Gets a value indicating whether the background <see cref="Task"/> has finished yet.
        /// </summary>
        public bool IsRunning => this.task.Status == TaskStatus.Running;

        /// <summary>
        /// Enqueues an <see cref="EventQueueClosingEvent"/>.
        /// </summary>
        public void BeginClose() => this.Enqueue(new EventQueueClosingEvent());

        #endregion
    }
}
