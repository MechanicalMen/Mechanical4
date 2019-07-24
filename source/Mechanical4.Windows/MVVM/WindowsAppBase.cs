using System;
using Mechanical4.EventQueue;
using Mechanical4.Misc;
using Mechanical4.MVVM;
using Microsoft.Win32;

namespace Mechanical4.Windows.MVVM
{
    /// <summary>
    /// The base class to use instead of <see cref="AppBase"/>, on Windows.
    /// </summary>
    public abstract class WindowsAppBase : AppBase
    {
        #region Initialization

        /// <summary>
        /// Initializes the (Mechanical4) app.
        /// </summary>
        /// <param name="eventQueue">The <see cref="EventQueueBase"/> to wrap.</param>
        protected static new void Initialize( EventQueueBase eventQueue )
        {
            AppBase.Initialize(eventQueue);

            InitializeUnhandledExceptionCatching();
            InitializeSystemEvents();
        }

        #endregion

        #region AppDomain unhandled exceptions

        private static void InitializeUnhandledExceptionCatching()
        {
            AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
        }

        private static void OnAppDomainUnhandledException( object sender, UnhandledExceptionEventArgs e )
        {
            if( !(e.ExceptionObject is Exception exception) )
            {
                // while in C# what you throw must be an Exception, the CLR does not require this (see: https://stackoverflow.com/questions/913472/why-is-unhandledexceptioneventargs-exceptionobject-an-object-and-not-an-exception)
                exception = new Exception($"Exception object is not an Exception! ({e.ExceptionObject?.ToString()})");
            }

            if( e.IsTerminating )
            {
                // save critical work first, hopefully handle regular events afterwards
                MainEventQueue.EventHandling.Suspend();
                try
                {
                    MainEventQueue.HandleCritical(new CriticalUnhandledExceptionEvent(exception));
                    MoveToState(AppState.Shutdown);
                }
                finally
                {
                    MainEventQueue.EventHandling.Resume();
                }
            }
            else
            {
                EnqueueException(exception);
            }
        }

        #endregion

        //// NOTE: TaskScheduler exceptions were handled in AppBase

        #region System events

        /* NOTE: This is how my Windows 10 machine behaved:
         * 
         * Shutdown & Reboot:
         *  - SessionEnding (cancellable; Reason: SystemShutdown)
         *  - SessionEnded (Reason: SystemShutdown)
         * 
         * Sign out / Log off:
         *  - SessionEnding (cancellable; Reason: Logoff)
         *  - SessionEnded (Reason: Logoff)
         * 
         * Lock & log back in:
         *  - SessionSwitch (Reason: SessionLock)
         *  - SessionSwitch (Reason: SessionUnlock)
         * 
         * Switch user & log back in:
         *  - SessionSwitch (Reason: ConsoleDisconnect)
         *  - SessionSwitch (Reason: ConsoleConnect)
         * 
         * Sleep & resume:
         *  - PowerModeChanged (Mode: Suspend)
         *  - PowerModeChanged (Mode: Resume)
         * 
         * Lock + Sleep:
         *  - SessionSwitch (Reason: SessionLock)
         *  - PowerModeChanged (Mode: Suspend)
         *  - PowerModeChanged (Mode: Resume)
         *  - SessionSwitch (Reason: SessionUnlock)
         *  
         * Switch user + Sleep:
         *  - SessionSwitch (Reason: ConsoleDisconnect)
         *  - PowerModeChanged (Mode: Suspend)
         *  - PowerModeChanged (Mode: Resume)
         *  - SessionSwitch (Reason: ConsoleConnect)
         */

        private static readonly ThreadSafeEnum<AppState> stateBeforeLeaving = new ThreadSafeEnum<AppState>();
        private static AppState StateBeforeLeaving
        {
            get => stateBeforeLeaving.GetCopy();
            set => stateBeforeLeaving.Set(value);
        }

        private static void Suspend()
        {
            StateBeforeLeaving = CurrentState;
            MoveToState(AppState.Suspended);
        }

        private static void Resume()
        {
            MoveToState(StateBeforeLeaving);
        }

        private static void InitializeSystemEvents()
        {
            SystemEvents.SessionEnded += OnSessionEnded;
            SystemEvents.SessionSwitch += OnSessionSwitch;
            SystemEvents.PowerModeChanged += OnPowerModeChanged;
        }

        private static void OnSessionEnded( object sender, SessionEndedEventArgs e )
        {
            // shutdown, reboot or log off
            MoveToState(AppState.Shutdown);
        }

        private static void OnSessionSwitch( object sender, SessionSwitchEventArgs e )
        {
            switch( e.Reason )
            {
            case SessionSwitchReason.SessionLock: // lock
            case SessionSwitchReason.ConsoleDisconnect: // switch user
                Suspend();
                break;

            case SessionSwitchReason.SessionUnlock: // sign in, after lock
            case SessionSwitchReason.ConsoleConnect: // sign in, after switch user
                Resume();
                break;
            }
        }

        private static void OnPowerModeChanged( object sender, PowerModeChangedEventArgs e )
        {
            switch( e.Mode )
            {
            case PowerModes.Suspend: // sleep
                Suspend();
                break;

            case PowerModes.Resume: // resume from sleep
                Resume();
                break;
            }
        }

        #endregion
    }
}
