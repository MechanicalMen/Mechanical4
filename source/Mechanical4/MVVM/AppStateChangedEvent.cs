using System;
using Mechanical4.EventQueue;

namespace Mechanical4.MVVM
{
    /// <summary>
    /// The events automatically raised on <see cref="AppState"/> transitions.
    /// </summary>
    public static class AppStateChangedEvent
    {
        #region Regular

        /// <summary>
        /// A single instance is raised after any state change, but not necessarily for every state transition
        /// (e.g. only one instance is enqueued after Running-->Suspended and Shutdown-->Suspended-->Running transitions).
        /// </summary>
        public class Regular : EventBase
        {
            private readonly MainEventQueue mainEventQueue;

            internal Regular( MainEventQueue mainQueue )
            {
                this.mainEventQueue = mainQueue ?? throw Exc.Null(nameof(mainQueue));
            }

            /// <summary>
            /// Gets the current application state.
            /// This property is updated immediately after an <see cref="AppStateChangedEvent.Critical"/> finished being handled.
            /// </summary>
            public AppState CurrentState => this.mainEventQueue.LastStateFinishedHandling;
        }

        #endregion

        #region Critical

        /// <summary>
        /// Raised when the application has transitioned into a new <see cref="AppState"/>.
        /// This is a critical event, so handling it may be time sensitive. Handle only what
        /// you absolutely must here, and leave the rest for <see cref="AppStateChangedEvent.Regular"/>.
        /// </summary>
        public class Critical : EventBase, ICriticalEvent
        {
            #region Constructor

            internal Critical( AppState oldState, AppState newState )
            {
                if( !Enum.IsDefined(typeof(AppState), oldState) )
                    throw new ArgumentException($"Invalid value! ({nameof(oldState)}: {oldState})");

                if( !Enum.IsDefined(typeof(AppState), newState) )
                    throw new ArgumentException($"Invalid value! ({nameof(newState)}: {newState})");

                if( Math.Abs((int)oldState - (int)newState) != 1 )
                    throw new ArgumentException($"Invalid state transition: {oldState} --> {newState}!");

                this.OldState = oldState;
                this.NewState = newState;
            }

            #endregion

            #region Public Properties

            /// <summary>
            /// Gets the old application state that was left.
            /// </summary>
            public AppState OldState { get; }

            /// <summary>
            /// Gets the new application state that was entered.
            /// </summary>
            public AppState NewState { get; }

            #endregion
        }

        #endregion
    }
}
