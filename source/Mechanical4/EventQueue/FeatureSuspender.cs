using System;

namespace Mechanical4.EventQueue
{
    /// <summary>
    /// Allows keeping track of the availability of a feature,
    /// when multiple sources may simultanously try to suspend it for a while.
    /// Basically a wrapper around a reference counter.
    /// Thread-safe.
    /// </summary>
    public class FeatureSuspender
    {
        #region Private Fields

        private readonly object syncLock = new object();
        private readonly Action suspended;
        private readonly Action resumed;
        private int refCount = 0;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureSuspender"/> class.
        /// The feature is initially enabled.
        /// </summary>
        /// <param name="onSuspended">Invoked when the feature was suspended, from the suspending thread.</param>
        /// <param name="onResumed">Invoked when the feature was resumed, from the resuming thread.</param>
        public FeatureSuspender( Action onSuspended = null, Action onResumed = null )
        {
            this.suspended = onSuspended;
            this.resumed = onResumed;
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Gets a value indicating whether the feature is enabled.
        /// </summary>
        public bool IsEnabled
        {
            get
            {
                lock( this.syncLock )
                    return this.refCount == 0;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the feature is suspended.
        /// </summary>
        public bool IsSuspended => !this.IsEnabled;

        /// <summary>
        /// Suspends the feature.
        /// </summary>
        public void Suspend()
        {
            lock( this.syncLock )
            {
                // increase counter
                ++this.refCount;

                // notify: feature suspended
                if( this.refCount == 1 )
                    this.suspended?.Invoke();
            }
        }

        /// <summary>
        /// Makes the feature available, as long as no other source suspended it.
        /// </summary>
        public void Resume()
        {
            lock( this.syncLock )
            {
                // decrease counter until zero
                if( this.refCount != 0 )
                    --this.refCount;

                // notify: feature resumed
                if( this.refCount == 0 )
                    this.resumed?.Invoke();
            }
        }

        #endregion
    }
}
