using System.Collections.Generic;

namespace Mechanical4.EventQueue.Primitives
{
    /// <summary>
    /// Thread-safe, first-in first-out event storage.
    /// </summary>
    public class FIFOEventStorage : IEventQueueStorage
    {
        #region Private Fields

        private readonly object syncLock = new object();
        private readonly List<EventBase> events = new List<EventBase>();

        #endregion

        #region IEventQueueStorage

        /// <summary>
        /// Tries to add the specified event to the storage.
        /// The storage may refuse to store certain events
        /// (e.g. multiple <see cref="ShuttingDownEvent"/>s).
        /// </summary>
        /// <param name="evnt">The event to enqueue.</param>
        /// <returns><c>true</c> if the event was added to the storage; otherwise, <c>false</c>.</returns>
        public bool TryPush( EventBase evnt )
        {
            if( evnt.NullReference() )
                throw Exc.Null(nameof(evnt));

            lock( this.syncLock )
            {
                this.events.Add(evnt);
                return true;
            }
        }

        /// <summary>
        /// Tries to remove an event from the storage.
        /// Returns <c>null</c> if the storage was empty.
        /// </summary>
        /// <returns>The event removed, or <c>null</c>.</returns>
        public EventBase TryPop()
        {
            lock( this.syncLock )
            {
                EventBase result = null;
                if( this.events.Count != 0 )
                {
                    result = this.events[0];
                    this.events.RemoveAt(0);
                }
                return result;
            }
        }

        /// <summary>
        /// Determines whether the specified event reference is stored.
        /// </summary>
        /// <param name="evnt">The event to search for within the storage.</param>
        /// <returns><c>true</c> if the specified event reference was found; otherwise, <c>false</c>.</returns>
        public bool Contains( EventBase evnt )
        {
            if( evnt.NullReference() )
                return false;

            lock( this.syncLock )
            {
                bool found = false;
                foreach( var e in this.events )
                {
                    if( ReferenceEquals(e, evnt) )
                    {
                        found = true;
                        break;
                    }
                }
                return found;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this storage is empty.
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                lock( this.syncLock )
                    return this.events.Count == 0;
            }
        }

        #endregion
    }
}
