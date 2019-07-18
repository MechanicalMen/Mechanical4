using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Mechanical4.EventQueue;
using Mechanical4.Misc;

namespace Mechanical4.Windows.MVVM.WPF
{
    /// <summary>
    /// Handles common, WPF boilerplate code.
    /// </summary>
    public class WpfApp : WindowsAppBase
    {
        private static WpfApp instance = null;

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="WpfApp"/> class.
        /// Uses a background thread to handle regular events.
        /// </summary>
        public WpfApp()
            : this(new TaskEventQueue())
        {
            // if there is one object that needs to stay around, it's this one.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WpfApp"/> class.
        /// </summary>
        /// <param name="eventQueue">The event queue to use for handling regular events. May not be <c>null</c>.</param>
        public WpfApp( EventQueueBase eventQueue )
            : base(eventQueue)
        {
            if( Interlocked.CompareExchange(ref instance, this, comparand: null).NotNullReference() )
                throw new InvalidOperationException($"There may be only 1 instance of {nameof(WpfApp)}, at most!");
        }

        /// <summary>
        /// Creates a new <see cref="WpfApp"/> instance,
        /// using a <see cref="TaskEventQueue"/> as the main event queue.
        /// </summary>
        /// <returns>A new <see cref="WpfApp"/> instance.</returns>
        public static WpfApp FromTaskEventQueue()
        {
            return new WpfApp(new TaskEventQueue());
        }

        #endregion

        #region UI Dispatcher Exception Handling

        private readonly WeakRefCollection<Dispatcher> dispatchersRaisingUnhandledEvents = new WeakRefCollection<Dispatcher>();

        /// <summary>
        /// Enqueues <see cref="UnhandledExceptionEvent"/> for unhandled exceptions thrown on the specified <see cref="Dispatcher"/>.
        /// </summary>
        /// <param name="dispatcher">The dispatcher to catch unhandled exceptions from, or <c>null</c> for the UI dispatcher.</param>
        /// <param name="raiseEvents"><c>true</c> to make sure unhandled exceptions are enqueued; <c>false</c> to make sure they are not.</param>
        public void RaiseUnhandledExceptionEventsFromDispatcher( Dispatcher dispatcher = null, bool raiseEvents = true )
        {
            if( dispatcher.NullReference() )
            {
                dispatcher = Application.Current?.Dispatcher;
                if( dispatcher.NullReference() )
                    throw new InvalidOperationException("Could not determine UI dispatcher! (Application.Current is not available)");
            }

            lock( this.dispatchersRaisingUnhandledEvents )
            {
                if( raiseEvents )
                {
                    if( !this.dispatchersRaisingUnhandledEvents.Contains(dispatcher) )
                    {
                        this.dispatchersRaisingUnhandledEvents.Add(dispatcher);
                        dispatcher.UnhandledException += OnDispatcherUnhandledException;
                    }
                }
                else
                {
                    if( this.dispatchersRaisingUnhandledEvents.Remove(dispatcher) )
                        dispatcher.UnhandledException -= OnDispatcherUnhandledException;
                }
            }
        }

        private void OnDispatcherUnhandledException( object sender, DispatcherUnhandledExceptionEventArgs e )
        {
            this.MainEventQueue.EnqueueRegular(new UnhandledExceptionEvent(e.Exception));
            e.Handled = true;
        }

        #endregion

        #region Window Closing

        //// NOTE: We are binding the window to the application life cycle:
        ////        - the window can not be closed before the application is ready to be closed
        ////        - closing the window means the application should be closed
        ////        - if the application's exiting, it should also close the window

        /* NOTE: here are the different events we need to handle:
         *  - the user tries to close the window (Window.Closing event), cancellable
         *  - some code requested for the application to start shutting down (ShutdownRequestEvent), cancellable
         *  - the main event queue is shutting down (ShuttingDownEvent, ShutDownEvent)
         *  - the application life cycle is coming to a close (AppStateChangedEvent.Regular, indicating AppState.Shutdown)
         * 
         * There are some overlaps however:
         *  - the application life cycle, and the main event queue life cycle are linked: it's enough to handle one of them
         *  - the cancellable events should be handled as one
         * 
         * The window life cycle will be as follows:
         *  - user initiated closing will always be immediately cancelled, but a ShutdownRequest event is enqueued,
         *    which may eventually lead to a ShutDownEvent.
         *  - ShutDownEvent is used to detect when the window is to be closed.
         */

        private class WindowCloseHandler : IEventHandler<ShutDownEvent>
        {
            private readonly WeakReference<Window> window;
            private bool canCloseWindow = false;

            internal WindowCloseHandler( Window wnd )
            {
                this.window = new WeakReference<Window>(wnd);
                wnd.Closing += this.Window_Closing;
            }

            private void Window_Closing( object sender, CancelEventArgs e )
            {
                // don't close the window, unless the event queue has already shut down
                if( !this.canCloseWindow )
                {
                    e.Cancel = true;
                    WpfApp.instance.MainEventQueue.RequestShutdown(); // implicitly enqueues ShuttingDownEvent, if not cancelled
                }
            }

            public void Handle( ShutDownEvent evnt )
            {
                if( !this.window.TryGetTarget(out var wnd) )
                    return;

                wnd.Dispatcher.BeginInvoke( // we handle thread-safety, by running all our code on the UI
                    new Action(() =>
                    {
                        this.canCloseWindow = true;
                        wnd.Close();
                    }),
                    DispatcherPriority.Background);
            }
        }

        /// <summary>
        /// The window will not be closed while the main event queue is running,
        /// and shutting down the main event queue will shut down the window as well.
        /// </summary>
        /// <param name="window">The <see cref="Window"/> to bind to the main event queue.</param>
        public void BindWindowToAppLifeCycle( Window window )
        {
            if( window.NullReference() )
                throw new ArgumentNullException(nameof(window)).StoreFileLine();

            var windowCloseHandler = new WindowCloseHandler(window);
            this.MainEventQueue.Subscribers.Add<ShutDownEvent>(windowCloseHandler, weakRef: false); // a strong ref. for the handler, but a weak ref. for the window
        }

        #endregion
    }
}
