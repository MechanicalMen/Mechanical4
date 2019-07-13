using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Mechanical4.EventQueue;

namespace Mechanical4.MVVM
{
    /// <summary>
    /// A base class for handling application level tasks.
    /// </summary>
    public abstract class AppBase
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="AppBase"/> class.
        /// </summary>
        /// <param name="eventQueue">The <see cref="EventQueueBase"/> to wrap.</param>
        protected AppBase( EventQueueBase eventQueue )
        {
            this.MainEventQueue = new MainEventQueue(eventQueue);
            this.MainEventQueue.Subscribers.AddAll(this, weakRef: false); // if there is one object that needs to stay around, it's this one.

            this.InitializeUnhandledExceptionCatching();
        }

        #endregion

        /// <summary>
        /// Gets the main event queue of the app.
        /// </summary>
        public MainEventQueue MainEventQueue { get; }

        #region App state handling

        /// <summary>
        /// Gets the current application state.
        /// This property is updated immediately after an <see cref="AppStateChangedEvent.Critical"/> finished being handled.
        /// </summary>
        public AppState CurrentState => this.MainEventQueue.LastStateFinishedHandling;

        /// <summary>
        /// Performs the necessary amount of state transitions, to reach the specified <see cref="AppState"/>.
        /// The current thread will be used, to have the required critical events (see <see cref="AppStateChangedEvent.Critical"/>) handled,
        /// while a single regular event will be enqueued, and handled as any other event.
        /// </summary>
        /// <param name="newState">The new <see cref="AppState"/> to set.</param>
        /// <param name="file">The source file of the caller.</param>
        /// <param name="member">The method or property name of the caller to this method.</param>
        /// <param name="line">The line number in <paramref name="file"/>.</param>
        public void MoveToState(
            AppState newState,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
        {
            this.MainEventQueue.MoveToState(newState, file, member, line);
        }

        #endregion

        #region Unhandled exception catching

        private void InitializeUnhandledExceptionCatching()
        {
            TaskScheduler.UnobservedTaskException += this.OnTaskSchedulerUnobservedTaskException;
        }

        private void OnTaskSchedulerUnobservedTaskException( object sender, UnobservedTaskExceptionEventArgs e )
        {
            //// NOTE: this will not be raised until the GC collects the unobserved tasks.

            this.MainEventQueue.EnqueueRegular(new UnhandledExceptionEvent(e.Exception));
            e.SetObserved();
        }

        #endregion
    }
}
