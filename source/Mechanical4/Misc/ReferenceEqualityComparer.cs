using System;
using System.Collections.Generic;

namespace Mechanical4.Misc
{
    /// <summary>
    /// An <see cref="IEqualityComparer{T}"/> that uses reference equality.
    /// <see cref="GetHashCode(T)"/> alwas throws exception, so this can not be used in <see cref="Dictionary{TKey, TValue}"/> and <see cref="HashSet{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of items to compare.</typeparam>
    public class ReferenceEqualityComparer<T> : IEqualityComparer<T>
        where T : class
    {
        /// <summary>
        /// The default instance of the type.
        /// </summary>
        public static readonly ReferenceEqualityComparer<T> Default = new ReferenceEqualityComparer<T>();

        /// <summary>
        /// Determines whether the specified objects are equal.
        /// </summary>
        /// <param name="x">The first object of type <typeparamref name="T"/> to compare.</param>
        /// <param name="y">The second object of type <typeparamref name="T"/> to compare.</param>
        /// <returns><c>true</c> if the specified objects are equal; otherwise, <c>false</c>.</returns>
        public bool Equals( T x, T y )
        {
            return ReferenceEquals(x, y);
        }

        /// <summary>
        /// This operation is not supported.
        /// </summary>
        /// <param name="obj">The object for which a hash code is to be returned.</param>
        /// <returns>A hash code for the specified object.</returns>
        /// <exception cref="NotSupportedException">Always raised.</exception>
        public int GetHashCode( T obj )
        {
            throw new NotSupportedException();

            //// NOTE: we could try to get the memory address of the class, which should be unique,
            ////       but I don't see it being worth the effort. (https://stackoverflow.com/questions/4994277/memory-address-of-an-object-in-c-sharp#10861731)
        }
    }
}
