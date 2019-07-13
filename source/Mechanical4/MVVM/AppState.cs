namespace Mechanical4.MVVM
{
    /// <summary>
    /// The stages of a generic, platform independent application life cycle.
    /// </summary>
    public enum AppState
    {
        /* TL;DR:
         * Basically: use regular event handlers by default,
         * and use critical event handlers, carefully, only when you need to.
         * 
         * 
         * NOTES:
         * These are the only accepted transitions: Running <--> Suspended <--> Shutdown
         * Intermediate stages must not be skipped!
         * 
         * We can get away with so few states, since they have nothing to do with the visual state
         * (e.g. visible, hidden, notifications only), which can be platform specific.
         * 
         * There are two kinds of events raised on state change:
         *  - critical: raised for each individual state transition. Handlers have only limited
         *    time to perform their tasks!
         *  - regular: raised sometime after the state change finished. As a regular event,
         *    handlers may choose to do heavier work, though care should be taken that the
         *    event queue is not stalled.
         * 
         * If you need to save critical information before the application is shut down,
         * then you should do so by handling the critical event, as long as doing so
         * can be done near instantly. For a larger amount of data, you should either use
         * the regular event (which may not get a chance to be handled), or you could
         * periodically stream changes to the disk (some kind of auto-save feature).
         * 
         * Here are some examples of what states a platform specific event may induce:
         * 
         * Starting an application:
         *  - [critical] Shutdown --> Suspended (bootstrap)
         *  - [critical] Suspended --> Running (resume basic services like logging)
         *  - [regular] state change (load the state; perhaps display a loading indicator or splash screen)
         * 
         * Gracefully forcing an application to close:
         *  - [critical] Running --> Suspended (flush small & critical info to disk)
         *  - [critical] Suspended --> Shutdown
         *  - [regular] state change (save whatever you need, and attempt to close the application normally)
         *  - [regular] shutting down event
         *  - <all events in the queue are processed, and the queue is shut down>
         * 
         * Gracefully closing an application, with user interaction:
         *  - user clicks close button
         *  - [regular] shutdown request (see if there are changes that might need saving)
         *     - <change detected: cancel request (since doing the things below may easily involve events)>
         *     - <ask the user whether to save or discard>
         *     - <optionally save changes>
         *  - <if the request is not cancelled: see above>
         * 
         * Edge case: two state changes in quick succession (e.g. logging off then on; or logging off, then shutting things down)
         *  - [critical] Running --> Suspended
         *  - regular event handling is processing events enqueued before state change
         *  - [critical] Suspended --> Running
         *  - [regular] state change (enqueued after the first critical event)
         *  - [regular] state change (enqueued after the second critical event)
         */

        /// <summary>
        /// The application is up and running.
        /// It may be in the foreground, or it may only perform background work.
        /// </summary>
        Running,

        /// <summary>
        /// A suspended application does not get scheduled on the processor.
        /// It is not known how long it will stay that way,
        /// or if it will ever leave this state.
        /// </summary>
        Suspended,

        /// <summary>
        /// The application has either just started, or is about to be terminated.
        /// In the case of the latter, the main event queue will automatically
        /// begin shutting down.
        /// </summary>
        Shutdown,
    }
}
