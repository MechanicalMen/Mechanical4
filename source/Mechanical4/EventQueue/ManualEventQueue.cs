using System;
using System.Collections.Generic;
using Mechanical4.EventQueue.Primitives;

namespace Mechanical4.EventQueue
{
    /// <summary>
    /// A thread-safe implementation of <see cref="IEventQueue"/>.
    /// Events are stored, until they are handled one-by-on using <see cref="HandleNext()"/>.
    /// Events are handled on the thread calling <see cref="HandleNext()"/>, not the one calling <see cref="IEventQueue.Enqueue"/>.
    /// </summary>
    public class ManualEventQueue : EventQueueBase
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ManualEventQueue"/> class.
        /// </summary>
        /// <param name="eventStorage">The <see cref="IEventQueueStorage"/> to use, or <c>null</c> to use a <see cref="FIFOEventStorage"/>.</param>
        /// <param name="subscriberCollection">The <see cref="EventSubscriberCollection"/> to use, or <c>null</c> to create a new one.</param>
        public ManualEventQueue(
            IEventQueueStorage eventStorage = null,
            EventSubscriberCollection subscriberCollection = null )
            : base(eventStorage, subscriberCollection)
        {
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Tries to invoke the event handlers of the next event.
        /// May fail if the event storage is empty, event handling is suspended,
        /// or if <see cref="EventQueueBase.BeforeAdding"/> returned <see cref="EventQueueBase.HandlingOption.UnableToHandle"/>.
        /// </summary>
        /// <returns><c>true</c> if an event was handled successfully; otherwise, <c>false</c>.</returns>
        public bool HandleNext()
        {
            return this.TryHandleNextEvent(out _);
        }

        /// <summary>
        /// Tries to invoke the event handlers of the next event.
        /// May fail if the event storage is empty, event handling is suspended,
        /// or if <see cref="EventQueueBase.BeforeAdding"/> returned <see cref="EventQueueBase.HandlingOption.UnableToHandle"/>.
        /// </summary>
        /// <param name="evnt">The event that was handled; or <c>null</c>.</param>
        /// <param name="exceptions">An optional list used to store exceptions thrown by event handlers. It always stores the exceptions, independent of the current state of <see cref="IEventQueue.RaiseUnhandledEvents"/>.</param>
        /// <returns><c>true</c> if an event was handled successfully; otherwise, <c>false</c>.</returns>
        public bool HandleNext( out EventBase evnt, List<Exception> exceptions = null )
        {
            return this.TryHandleNextEvent(out evnt, exceptions);
        }

        #endregion
    }
}
