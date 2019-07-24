using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Mechanical4.EventQueue;
using Mechanical4.Misc;

namespace Mechanical4.MVVM
{
    /// <summary>
    /// A base class for handling application level tasks.
    /// </summary>
    public abstract class AppBase
    {
        #region Initialization

        private static readonly ThreadSafeBoolean isInitialized = new ThreadSafeBoolean(false);

        /// <summary>
        /// The constructor is not supported, please do not use it (use <see cref="Initialize"/> instead).
        /// </summary>
        protected AppBase()
        {
            throw new NotSupportedException("This is a static class in all but name, there is no need to construct instances.");
        }

        /// <summary>
        /// Initializes the (Mechanical4) app.
        /// </summary>
        /// <param name="eventQueue">The <see cref="EventQueueBase"/> to wrap.</param>
        protected static void Initialize( EventQueueBase eventQueue )
        {
            var wrapper = new MainEventQueue(eventQueue); // tests the argument implicitly

            isInitialized.SetIfEquals(newValue: true, comparand: false, out bool oldValue);
            if( oldValue )
                throw new InvalidOperationException("The (Mechanical4) app may only be initialized once!");

            mainEventQueue = wrapper;
            InitializeUnhandledExceptionCatching();
        }

        /// <summary>
        /// Throws an exception, if <see cref="Initialize"/> was not yet called.
        /// </summary>
        protected static void ThrowIfNotInitialized()
        {
            if( !isInitialized.GetCopy() )
                throw new InvalidOperationException("The (Mechanical4) app has not yet been initialized!");
        }

        #endregion

        #region MainEventQueue

        private static MainEventQueue mainEventQueue;

        /// <summary>
        /// Gets the main event queue of the app.
        /// </summary>
        public static MainEventQueue MainEventQueue
        {
            get
            {
                ThrowIfNotInitialized();

                return mainEventQueue;
            }
        }

        #endregion

        #region App state handling

        /// <summary>
        /// Gets the current application state.
        /// This property is updated immediately after an <see cref="AppStateChangedEvent.Critical"/> finished being handled.
        /// </summary>
        public static AppState CurrentState => MainEventQueue.LastStateFinishedHandling;

        /// <summary>
        /// Performs the necessary amount of state transitions, to reach the specified <see cref="AppState"/>.
        /// The current thread will be used, to have the required critical events (see <see cref="AppStateChangedEvent.Critical"/>) handled,
        /// while a single regular event will be enqueued, and handled as any other event.
        /// </summary>
        /// <param name="newState">The new <see cref="AppState"/> to set.</param>
        /// <param name="file">The source file of the caller.</param>
        /// <param name="member">The method or property name of the caller to this method.</param>
        /// <param name="line">The line number in <paramref name="file"/>.</param>
        public static void MoveToState(
            AppState newState,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
        {
            MainEventQueue.MoveToState(newState, file, member, line);
        }

        #endregion

        #region Unhandled exception catching

        private static void InitializeUnhandledExceptionCatching()
        {
            TaskScheduler.UnobservedTaskException += OnTaskSchedulerUnobservedTaskException;
        }

        private static void OnTaskSchedulerUnobservedTaskException( object sender, UnobservedTaskExceptionEventArgs e )
        {
            //// NOTE: this will not be raised until the GC collects the unobserved tasks.

            EnqueueException(e.Exception);
            e.SetObserved();
        }

        #endregion

        #region Unhandled exception handling

        /// <summary>
        /// Enqueues an <see cref="UnhandledExceptionEvent"/>.
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/> to have handled.</param>
        /// <param name="file">The source file of the caller.</param>
        /// <param name="member">The method or property name of the caller to this method.</param>
        /// <param name="line">The line number in <paramref name="file"/>.</param>
        /// <returns><c>true</c> if the event was enqueued successfully; otherwise, <c>false</c>.</returns>
        public static bool EnqueueException(
            Exception exception,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
        {
            if( exception.NullReference() )
                return false; // exception handling is the one place we may want to silence issues.

            exception.StoreFileLine(file, member, line);
            return MainEventQueue.EnqueueRegular(new UnhandledExceptionEvent(exception), file, member, line);
        }

        #endregion
    }
}
