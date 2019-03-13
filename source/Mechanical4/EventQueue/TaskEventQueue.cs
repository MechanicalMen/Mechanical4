using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mechanical4.EventQueue.Primitives;

namespace Mechanical4.EventQueue
{
    /// <summary>
    /// Uses a long running task to execute event handlers as events become available.
    /// </summary>
    public class TaskEventQueue : EventQueueBase
    {
        #region Private Fields

        private readonly ManualResetEventSlim eventAvailableWaitHandle;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskEventQueue"/> class.
        /// </summary>
        /// <param name="eventStorage">The <see cref="IEventQueueStorage"/> to use, or <c>null</c> to use a <see cref="FIFOEventStorage"/>.</param>
        public TaskEventQueue( IEventQueueStorage eventStorage = null )
            : base(eventStorage, subscriberCollection: null)
        {
            this.eventAvailableWaitHandle = new ManualResetEventSlim(initialState: false); // nonsignaled, blocks
            this.Task = Task.Factory.StartNew(this.Work, TaskCreationOptions.LongRunning);
        }

        #endregion

        #region Private Methods

        private void Work()
        {
            var exceptions = new List<Exception>();
            while( true )
            {
                // wait for the next Enqueue call
                this.eventAvailableWaitHandle.Wait(); // wait indefinitely for events to be added
                this.SuspendTask(); // wait again after we handled all available events

                // handle all that we can
                while( this.TryHandleNextEvent(out _) ) ;

                // finish if shut down
                if( this.IsShutDown )
                    break;
            }
        }

        private void SuspendTask() => this.eventAvailableWaitHandle.Reset(); // nonsignaled, blocks
        private void ResumeTask() => this.eventAvailableWaitHandle.Set(); // signaled, does not block

        #endregion

        #region EventQueueBase

        /// <summary>
        /// Invoked after the event was successfully added to the <see cref="IEventQueueStorage"/>.
        /// </summary>
        /// <param name="evnt">The event that was added.</param>
        protected override void AfterAdding( EventBase evnt )
        {
            if( this.EventHandling.IsEnabled )
                this.ResumeTask();
        }

        /// <summary>
        /// Invoked when the queue is suspended.
        /// </summary>
        protected override void OnHandlingSuspended()
        {
            this.SuspendTask();
            base.OnHandlingSuspended();
        }

        /// <summary>
        /// Invoked when the queue is resumed.
        /// </summary>
        protected override void OnHandlingResumed()
        {
            base.OnHandlingResumed();
            this.ResumeTask();
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Gets the task doing the event processing.
        /// </summary>
        public Task Task { get; }

        #endregion
    }
}
