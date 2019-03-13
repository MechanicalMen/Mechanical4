using System;
using System.Reflection;
using System.Threading;

namespace Mechanical4.Misc
{
    /// <summary>
    /// Provides thread-safe access to an &quot;enum&quot; variable.
    /// </summary>
    public class ThreadSafeEnum<T>
        where T : System.Enum
    {
        #region Private Fields

        private static readonly bool HasSignedUnderlyingType;
        private static readonly bool HasFlagsAttribute;

        private long signedValue;

        #endregion

        #region Constructor

        static ThreadSafeEnum()
        {
            var type = Enum.GetUnderlyingType(typeof(T));
            HasSignedUnderlyingType = type == typeof(sbyte)
                                   || type == typeof(short)
                                   || type == typeof(int)
                                   || type == typeof(long);

            HasFlagsAttribute = typeof(T).GetTypeInfo().GetCustomAttribute(typeof(FlagsAttribute), inherit: false).NotNullReference();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadSafeBoolean"/> class.
        /// </summary>
        /// <param name="initialValue">The initial value to hold.</param>
        public ThreadSafeEnum( T initialValue = default )
        {
            this.Set(initialValue);
        }

        #endregion

        #region Private Methods

        //// NOTE: Unfortunately conversion involves boxing.
        ////       We could avoid this through dynamic compilation.

        private static long Convert( T value )
        {
            if( HasSignedUnderlyingType )
                return System.Convert.ToInt64(value);
            else
                return ToSigned(System.Convert.ToUInt64(value));
        }

        private static T Convert( long value )
        {
            return (T)Enum.ToObject(typeof(T), HasSignedUnderlyingType ? (object)value : (object)ToUnsigned(value));
        }

        private static long ToSigned( ulong value )
        {
            unchecked
            {
                return (long)value;
            }
        }

        private static ulong ToUnsigned( long value )
        {
            unchecked
            {
                return (ulong)value;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets a copy of the current value.
        /// By the time you examine it however, it may be out of date.
        /// Use <see cref="SetIfEquals"/>, if you need to update it based on it's current value.
        /// </summary>
        /// <returns>The current value.</returns>
        public T GetCopy()
        {
            return Convert(Interlocked.CompareExchange(ref this.signedValue, 0L, 0L));
        }

        /// <summary>
        /// Changes the current value to the one specified.
        /// </summary>
        /// <param name="newValue">The new value to hold.</param>
        public void Set( T newValue ) => this.Set(newValue, out _);

        /// <summary>
        /// Changes the current value to the one specified.
        /// </summary>
        /// <param name="newValue">The new value to hold.</param>
        /// <param name="oldValue">The value that was overwritten.</param>
        public void Set( T newValue, out T oldValue )
        {
            if( !HasFlagsAttribute )
            {
                if( !Enum.IsDefined(typeof(T), newValue) )
                    throw new ArgumentOutOfRangeException(nameof(newValue), newValue, $"Invalid new value: {newValue}");
            }

            oldValue = Convert(Interlocked.Exchange(ref this.signedValue, Convert(newValue)));
        }

        /// <summary>
        /// Changes the current value, if the old value is equal to the one specified.
        /// </summary>
        /// <param name="newValue">The new value to hold.</param>
        /// <param name="comparand">The value to compare to.</param>
        /// <param name="oldValue">The value that may or may not have been overwritten.</param>
        public void SetIfEquals( T newValue, T comparand, out T oldValue )
        {
            if( !HasFlagsAttribute )
            {
                if( !Enum.IsDefined(typeof(T), newValue) )
                    throw new ArgumentOutOfRangeException(nameof(newValue), newValue, $"Invalid new value: {newValue}");
            }

            oldValue = Convert(
                Interlocked.CompareExchange(
                    ref this.signedValue,
                    Convert(newValue),
                    Convert(comparand)));
        }

        #endregion
    }
}
