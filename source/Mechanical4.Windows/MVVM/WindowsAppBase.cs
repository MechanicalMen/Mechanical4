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
        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsAppBase"/> class.
        /// </summary>
        /// <param name="eventQueue">The <see cref="EventQueueBase"/> to wrap.</param>
        protected WindowsAppBase( EventQueueBase eventQueue )
            : base(eventQueue)
        {
            this.InitializeUnhandledExceptionCatching();
            this.InitializeSystemEvents();
        }

        #region AppDomain unhandled exceptions

        private void InitializeUnhandledExceptionCatching()
        {
            AppDomain.CurrentDomain.UnhandledException += this.OnAppDomainUnhandledException;
        }

        private void OnAppDomainUnhandledException( object sender, UnhandledExceptionEventArgs e )
        {
            if( !(e.ExceptionObject is Exception exception) )
            {
                // while in C# what you throw must be an Exception, the CLR does not require this (see: https://stackoverflow.com/questions/913472/why-is-unhandledexceptioneventargs-exceptionobject-an-object-and-not-an-exception)
                exception = new Exception($"Exception object is not an Exception! ({e.ExceptionObject?.ToString()})");
            }

            if( e.IsTerminating )
            {
                // save critical work first, hopefully handle regular events afterwards
                this.MainEventQueue.EventHandling.Suspend();
                try
                {
                    this.MainEventQueue.EnqueueRegular(new CriticalUnhandledExceptionEvent(exception));
                    this.MoveToState(AppState.Shutdown);
                }
                finally
                {
                    this.MainEventQueue.EventHandling.Resume();
                }
            }
            else
            {
                this.MainEventQueue.EnqueueRegular(new UnhandledExceptionEvent(exception));
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

        private readonly ThreadSafeEnum<AppState> stateBeforeLeaving = new ThreadSafeEnum<AppState>();
        private AppState StateBeforeLeaving
        {
            get => this.stateBeforeLeaving.GetCopy();
            set => this.stateBeforeLeaving.Set(value);
        }

        private void Suspend()
        {
            this.StateBeforeLeaving = this.CurrentState;
            this.MoveToState(AppState.Suspended);
        }

        private void Resume()
        {
            this.MoveToState(this.StateBeforeLeaving);
        }

        private void InitializeSystemEvents()
        {
            SystemEvents.SessionEnded += this.OnSessionEnded;
            SystemEvents.SessionSwitch += this.OnSessionSwitch;
            SystemEvents.PowerModeChanged += this.OnPowerModeChanged;
        }

        private void OnSessionEnded( object sender, SessionEndedEventArgs e )
        {
            // shutdown, reboot or log off
            this.MoveToState(AppState.Shutdown);
        }

        private void OnSessionSwitch( object sender, SessionSwitchEventArgs e )
        {
            switch( e.Reason )
            {
            case SessionSwitchReason.SessionLock: // lock
            case SessionSwitchReason.ConsoleDisconnect: // switch user
                this.Suspend();
                break;

            case SessionSwitchReason.SessionUnlock: // sign in, after lock
            case SessionSwitchReason.ConsoleConnect: // sign in, after switch user
                this.Resume();
                break;
            }
        }

        private void OnPowerModeChanged( object sender, PowerModeChangedEventArgs e )
        {
            switch( e.Mode )
            {
            case PowerModes.Suspend: // sleep
                this.Suspend();
                break;

            case PowerModes.Resume: // resume from sleep
                this.Resume();
                break;
            }
        }

        #endregion
    }
}
