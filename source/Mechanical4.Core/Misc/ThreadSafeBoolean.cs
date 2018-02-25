using System;
using System.Threading;

namespace Mechanical4.Core.Misc
{
    /// <summary>
    /// Mimics a <see cref="Boolean"/> variable, in a thread-safe manner.
    /// </summary>
    public class ThreadSafeBoolean
    {
        //// NOTE: https://stackoverflow.com/questions/29411961/c-sharp-and-thread-safety-of-a-bool

        private const int FALSE = 0;
        private const int TRUE = 1;

        private int value = FALSE; // the default bool value

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadSafeBoolean"/> class.
        /// </summary>
        /// <param name="initialValue">The initial value to hold.</param>
        public ThreadSafeBoolean( bool initialValue = default(bool) )
        {
            this.Set(initialValue);
        }

        private static int Convert( bool value ) => value ? TRUE : FALSE;
        private static bool Convert( int value ) => value == TRUE ? true : false;

        /// <summary>
        /// Gets a copy of the current value.
        /// By the time you examine it however, it may be out of date.
        /// Use <see cref="SetIfEquals"/>, if you need to update it based on it's current value.
        /// </summary>
        /// <returns>The current value.</returns>
        public bool GetCopy()
        {
            this.SetIfEquals(false, false, out bool oldValue);
            return oldValue;
        }

        /// <summary>
        /// Changes the current value to the one specified.
        /// </summary>
        /// <param name="newValue">The new value to hold.</param>
        public void Set( bool newValue ) => this.Set(newValue, out _);

        /// <summary>
        /// Changes the current value to the one specified.
        /// </summary>
        /// <param name="newValue">The new value to hold.</param>
        /// <param name="oldValue">The value that was overwritten.</param>
        public void Set( bool newValue, out bool oldValue )
        {
            oldValue = Convert(Interlocked.Exchange(ref this.value, Convert(newValue)));
        }

        /// <summary>
        /// Changes the current value, if the old value is equal to the one specified.
        /// </summary>
        /// <param name="newValue">The new value to hold.</param>
        /// <param name="comparand">The value to compare to.</param>
        /// <param name="oldValue">The value that may or may not have been overwritten.</param>
        public void SetIfEquals( bool newValue, bool comparand, out bool oldValue )
        {
            oldValue = Convert(
                Interlocked.CompareExchange(
                    ref this.value,
                    Convert(newValue),
                    Convert(comparand)));
        }
    }
}
