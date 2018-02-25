using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Mechanical4.Core.Misc;

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
        private readonly ThreadSafeBoolean isSuspended;
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
            this.isSuspended = new ThreadSafeBoolean(false);
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
        /// There is no guarantee that the event will end up being handled
        /// (e.g. closed queues can not enqueue, and the application
        /// may be terminated beforehand).
        /// Suspended event queues can still enqueue events (see <see cref="IsSuspended"/>).
        /// </summary>
        /// <param name="evnt">The event to enqueue.</param>
        /// <param name="file">The source file of the caller.</param>
        /// <param name="member">The method or property name of the caller to this method.</param>
        /// <param name="line">The line number in <paramref name="file"/>.</param>
        public void Enqueue(
            EventBase evnt,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
        {
            this.manualQueue.Enqueue(evnt, file, member, line);

            if( !this.IsSuspended )
                this.newEventWaitHandle.Set(); // signaled, does not block
        }

        /// <summary>
        /// Gets the collection of event handlers.
        /// </summary>
        public EventSubscriberCollection Subscribers => this.manualQueue.Subscribers;

        /// <summary>
        /// Gets or sets a value indicating whether the handling of enqueued events is currently allowed.
        /// Suspension does not apply to event handling already in progress.
        /// </summary>
        public bool IsSuspended
        {
            get => this.isSuspended.GetCopy();
            set
            {
                this.isSuspended.Set(value, out bool oldValue);
                if( oldValue != value )
                {
                    // value changed
                    if( value )
                    {
                        // suspend
                        this.newEventWaitHandle.Reset(); // nonsignaled, blocks
                    }
                    else
                    {
                        // resume
                        this.newEventWaitHandle.Set(); // signaled, does not block
                    }
                }
            }
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Gets the task doing the event processing.
        /// </summary>
        public Task Task => this.task;

        #endregion
    }
}
