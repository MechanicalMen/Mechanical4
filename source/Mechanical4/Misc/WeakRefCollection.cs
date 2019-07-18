using System;
using System.Collections.Generic;

namespace Mechanical4.Misc
{
    /// <summary>
    /// A collection that stores weak references.
    /// Old references are automatically cleaned up.
    /// NOT thread-safe.
    /// </summary>
    /// <typeparam name="T">The type of objects to store weak reference to.</typeparam>
    public class WeakRefCollection<T>
        where T : class
    {
        #region Private Fields

        private readonly List<WeakReference<T>> list;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="WeakRefCollection{T}"/> class.
        /// </summary>
        public WeakRefCollection()
        {
            this.list = new List<WeakReference<T>>();
        }

        #endregion

        #region Private Methods

        private int IndexOf_Core( T comparand, IEqualityComparer<T> comparer )
        {
            for( int i = 0; i < this.list.Count; )
            {
                if( this.list[i].TryGetTarget(out T item) )
                {
                    if( comparer.Equals(item, comparand) )
                        return i;
                    else
                        ++i;
                }
                else
                {
                    this.list.RemoveAt(i);
                }
            }
            return -1;
        }

        private int IndexOf_List( T comparand, IEqualityComparer<T> comparer )
        {
            //// NOTE: this was based on the behavior of List<T>.Contains

            if( comparand.NullReference() )
                return this.IndexOf_Core(null, ReferenceEqualityComparer<T>.Default);
            else
                return this.IndexOf_Core(comparand, comparer ?? EqualityComparer<T>.Default);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds the specified <see cref="WeakReference{T}"/> to this collection.
        /// The <see cref="WeakReference{T}"/> itself is stored using a strong reference.
        /// </summary>
        /// <param name="weakRef">The <see cref="WeakReference{T}"/> to add.</param>
        public void Add( WeakReference<T> weakRef )
        {
            if( weakRef.NullReference() )
                throw Exc.Null(nameof(weakRef));

            //// NOTE: we are not checking whether the weak reference is still alive,
            ////       since the assumption is that it was recently created (e.g. by one of our other members).

            this.list.Add(weakRef);
        }

        /// <summary>
        /// Adds a weak reference to the specified object, to this collection.
        /// </summary>
        /// <param name="obj">The object to store a weak reference of.</param>
        public void Add( T obj )
        {
            if( obj.NullReference() )
                throw Exc.Null(nameof(obj));

            this.Add(new WeakReference<T>(obj));
        }

        /// <summary>
        /// Determines whether a weak reference to the specified object is in the collection.
        /// </summary>
        /// <param name="item">The object to locate a weak reference of.</param>
        /// <param name="comparer">The comparer to use, or <c>null</c> for <see cref="EqualityComparer{T}.Default"/>.</param>
        /// <returns><c>true</c> if a weak rerf</returns>
        public bool Contains( T item, IEqualityComparer<T> comparer = null )
        {
            return this.IndexOf_List(item, comparer) != -1; // this method imitates the behavior of List.Contains
        }

        /// <summary>
        /// Removes a weak reference of the specified object.
        /// </summary>
        /// <param name="item">The object to locate a weak reference of.</param>
        /// <param name="comparer">The comparer to use, or <c>null</c> for <see cref="EqualityComparer{T}.Default"/>.</param>
        /// <returns><c>true</c> if a weak reference was found and removed; <c>false</c> if a weak reference could not be found, or it was no longer alive.</returns>
        public bool Remove( T item, IEqualityComparer<T> comparer = null )
        {
            int index = this.IndexOf_List(item, comparer);
            if( index != -1 )
            {
                this.list.RemoveAt(index);
                return true;
            }
            return false;
        }

        #endregion
    }
}
