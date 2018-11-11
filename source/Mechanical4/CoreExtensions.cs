using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Mechanical4.Misc;

namespace Mechanical4
{
    /// <summary>
    /// Extension methods for the <see cref="Mechanical4"/> namespace.
    /// </summary>
    public static class CoreExtensions
    {
        //// NOTE: we don't want to "litter" intellisense with rarely used extension methods (especially on System.Object!), so think twice, before adding something here

        #region object

        /// <summary>
        /// Determines whether the object is <c>null</c>.
        /// </summary>
        /// <param name="value">The object to check.</param>
        /// <returns><c>true</c> if the specified object is <c>null</c>; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NullReference( this object value )
        {
            return object.ReferenceEquals(value, null);
        }

        /// <summary>
        /// Determines whether the object is not <c>null</c>.
        /// </summary>
        /// <param name="value">The object to check.</param>
        /// <returns><c>true</c> if the specified object is not <c>null</c>; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NotNullReference( this object value )
        {
            return !object.ReferenceEquals(value, null);
        }

        #endregion

        #region string

        /// <summary>
        /// Determines whether the string is <c>null</c> or empty.
        /// </summary>
        /// <param name="str">The string to check.</param>
        /// <returns><c>true</c> if the specified string is <c>null</c> or empty; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NullOrEmpty( this string str )
        {
            return string.IsNullOrEmpty(str);
        }

        /// <summary>
        /// Determines whether the string is <c>null</c>, empty, or if it has leading or trailing white-space characters.
        /// </summary>
        /// <param name="str">The string to check.</param>
        /// <returns><c>true</c> if the specified string is <c>null</c>, empty or lengthy; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NullOrLengthy( this string str )
        {
            return string.IsNullOrEmpty(str)
                || char.IsWhiteSpace(str, 0)
                || char.IsWhiteSpace(str, str.Length - 1);
        }

        /// <summary>
        /// Determines whether the string is <c>null</c>, empty, or if it consists only of white-space characters.
        /// </summary>
        /// <param name="str">The string to check.</param>
        /// <returns><c>true</c> if the specified string is <c>null</c>, empty, or whitespace; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NullOrWhiteSpace( this string str )
        {
            return string.IsNullOrWhiteSpace(str);
        }

        #endregion

        #region Collections

        //// NOTE: we extend IEnumerable<T>, because overloading can easily lead to ambiguity.
        //// NOTE: arrays implement ICollection<T>, IList<T> as well.

        /// <summary>
        /// Determines whether the specified sequence is <c>null</c> or empty.
        /// </summary>
        /// <typeparam name="T">The type of the elelements of <paramref name="sequence"/>.</typeparam>
        /// <param name="sequence">A sequence to test.</param>
        /// <returns><c>true</c> if <paramref name="sequence"/> is <c>null</c> or empty; otherwise <c>false</c>.</returns>
        public static bool NullOrEmpty<T>( this IEnumerable<T> sequence )
        {
            if( sequence.NullReference() )
                return true;

            var collection = sequence as ICollection<T>;
            if( collection.NotNullReference() )
                return collection.Count == 0;

            var readOnlyCollection = sequence as IReadOnlyCollection<T>;
            if( readOnlyCollection.NotNullReference() )
                return readOnlyCollection.Count == 0;

            var enumerator = sequence.GetEnumerator(); // this probably means an allocation, which we would like to avoid, if possible
            return !enumerator.MoveNext();
        }

        /// <summary>
        /// Determines whether the specified sequence is <c>null</c>, empty or whether it contains any <c>null</c> references.
        /// </summary>
        /// <typeparam name="T">The type of the elelements of <paramref name="sequence"/>.</typeparam>
        /// <param name="sequence">A sequence to test.</param>
        /// <returns><c>true</c> if <paramref name="sequence"/> is <c>null</c>, empty or contains at least one <c>null</c> reference; otherwise <c>false</c>.</returns>
        public static bool NullEmptyOrSparse<T>( this IEnumerable<T> sequence )
            where T : class
        {
            return sequence.NullOrEmpty()
                || sequence.Contains(item => item.NullReference());
        }

        /// <summary>
        /// Determines whether the specified sequence is <v>null</v> or whether it contains any <c>null</c> references.
        /// </summary>
        /// <typeparam name="T">The type of the elelements of <paramref name="sequence"/>.</typeparam>
        /// <param name="sequence">A sequence to test.</param>
        /// <returns><c>true</c> if <paramref name="sequence"/> is <c>null</c> or contains at least one <c>null</c> reference; otherwise <c>false</c>.</returns>
        public static bool NullOrSparse<T>( this IEnumerable<T> sequence )
            where T : class
        {
            return sequence.NullReference()
                || sequence.Contains(item => item.NullReference());
        }

        /// <summary>
        /// Determines whether a sequence contains a specified element by using the specified predicate.
        /// </summary>
        /// <typeparam name="T">The type of the elelements of <paramref name="sequence"/>.</typeparam>
        /// <param name="sequence">A sequence to search the elements of.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns><c>true</c> if an element in the sequence passed the test in the specified predicate function; otherwise, <c>false</c>.</returns>
        public static bool Contains<T>( this IEnumerable<T> sequence, Func<T, bool> predicate )
        {
            if( sequence.NullReference() )
                throw Exc.Null(nameof(sequence));

            if( predicate.NullReference() )
                throw Exc.Null(nameof(predicate));

            var list = sequence as IList<T>;
            if( list.NotNullReference() )
            {
                if( list.Count == 0 )
                    return false;

                for( int i = 0; i < list.Count; ++i )
                {
                    if( predicate(list[i]) )
                        return true;
                }

                return false;
            }

            var readOnlyList = sequence as IReadOnlyList<T>;
            if( readOnlyList.NotNullReference() )
            {
                if( readOnlyList.Count == 0 )
                    return false;

                for( int i = 0; i < readOnlyList.Count; ++i )
                {
                    if( predicate(readOnlyList[i]) )
                        return true;
                }

                return false;
            }

            var enumerator = sequence.GetEnumerator();
            if( !enumerator.MoveNext() )
            {
                return false;
            }
            else
            {
                do
                {
                    if( predicate(enumerator.Current) )
                        return true;
                }
                while( enumerator.MoveNext() );

                return false;
            }
        }

        /// <summary>
        /// Returns the first element in a sequence, or <c>null</c> if the sequence contains no elements.
        /// </summary>
        /// <typeparam name="T">The type of the elelements of <paramref name="sequence"/>.</typeparam>
        /// <param name="sequence">A sequence to get the first element of.</param>
        /// <returns>The first element in the sequence; or <c>null</c> if no such element was found.</returns>
        public static T? FirstOrNullable<T>( this IEnumerable<T> sequence )
            where T : struct
        {
            if( sequence.NullReference() )
                throw Exc.Null(nameof(sequence));

            var list = sequence as IList<T>;
            if( list.NotNullReference() )
            {
                if( list.Count != 0 )
                    return list[0];
                else
                    return null;
            }

            var readOnlyList = sequence as IReadOnlyList<T>;
            if( readOnlyList.NotNullReference() )
            {
                if( readOnlyList.Count != 0 )
                    return readOnlyList[0];
                else
                    return null;
            }

            var enumerator = sequence.GetEnumerator();
            if( enumerator.MoveNext() )
                return enumerator.Current;
            else
                return null;
        }

        /// <summary>
        /// Returns the first element in a sequence that satisfies a specified condition,
        /// or <c>null</c> if the sequence contains no elements.
        /// </summary>
        /// <typeparam name="T">The type of the elelements of <paramref name="sequence"/>.</typeparam>
        /// <param name="sequence">A sequence to search the elements of.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>The first element in the sequence that passes the test in the specified predicate function; or <c>null</c> if no such element was found.</returns>
        public static T? FirstOrNullable<T>( this IEnumerable<T> sequence, Func<T, bool> predicate )
            where T : struct
        {
            return sequence.Where(predicate).FirstOrNullable();
        }

        #endregion

        #region Exception

        /// <summary>
        /// Copies <see cref="Exception.Data"/> into an array of key-value pairs.
        /// </summary>
        /// <param name="e">The exception to use.</param>
        /// <returns>An array of key-value pairs.</returns>
        public static KeyValuePair<string, string>[] GetPairs( this Exception e )
        {
            var result = new List<KeyValuePair<string, string>>();
            foreach( DictionaryEntry entry in e.Data )
            {
                if( entry.Key is string key )
                {
                    // we only care about non-null string values, or null
                    if( entry.Value.NullReference() )
                    {
                        result.Add(new KeyValuePair<string, string>(key, null));
                    }
                    else if( entry.Value is string value ) // do not yield, if not a string
                    {
                        result.Add(new KeyValuePair<string, string>(key, value));
                    }
                }
            }
            return result.ToArray();
        }

        private static bool TryGetValue( Exception e, string key, out string value )
        {
            foreach( var pair in GetPairs(e) )
            {
                if( string.Equals(key, pair.Key, StringComparison.Ordinal) )
                {
                    value = pair.Value;
                    return true;
                }
            }

            value = null;
            return false;
        }

        private static void AddOrSet( Exception e, string key, string value )
        {
            e.Data[key] = value;
        }

        /// <summary>
        /// Stores the current source file position (using the "PartialStackTrace" key).
        /// </summary>
        /// <typeparam name="TException">The type of the exception.</typeparam>
        /// <param name="e">The exception to store data in.</param>
        /// <param name="file">The source file that contains the caller.</param>
        /// <param name="member">The method or property name of the caller to this method.</param>
        /// <param name="line">The line number in the source file at which this method is called.</param>
        /// <returns>The exception the data was stored in.</returns>
        public static TException StoreFileLine<TException>(
            this TException e,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
            where TException : Exception
        {
            const string key = "PartialStackTrace";
            string newValue = FileLine.Compact(file, member, line);
            if( TryGetValue(e, key, out var oldValue) )
            {
                if( !oldValue.NullOrEmpty() )
                    newValue = newValue + "\r\n" + oldValue;
            }
            AddOrSet(e, key, newValue);
            return e;
        }

        private static bool SameMemberAsTopPartialStackTrace( Exception e, string file, string member )
        {
            const string key = "PartialStackTrace";
            if( !TryGetValue(e, key, out string partialStackTrace) )
                return false;

            int firstLineEndsAt = partialStackTrace.IndexOf('\r');
            string firstLine = firstLineEndsAt == -1 ? partialStackTrace : partialStackTrace.Substring(0, firstLineEndsAt);

            string pstFile, pstMember;
            try
            {
                FileLine.ParseCompact(firstLine, out pstFile, out pstMember, out _);
            }
            catch
            {
                return false;
            }

            return string.Equals(pstFile, file, StringComparison.Ordinal)
                && string.Equals(pstMember, member, StringComparison.Ordinal);
        }

        /// <summary>
        /// Stores a key-value pair in <see cref="Exception.Data"/>.
        /// The <paramref name="key"/> may be changed slightly, if it is already in use.
        /// The first call in a member automatically invokes <see cref="StoreFileLine"/>.
        /// </summary>
        /// <typeparam name="TException">The type of the exception.</typeparam>
        /// <param name="e">The exception to store data in.</param>
        /// <param name="key">The key to store.</param>
        /// <param name="value">The value to store.</param>
        /// <param name="file">The source file that contains the caller.</param>
        /// <param name="member">The method or property name of the caller to this method.</param>
        /// <param name="line">The line number in the source file at which this method is called.</param>
        /// <returns>The exception the data was stored in.</returns>
        public static TException Store<TException>(
            this TException e,
            string key,
            string value,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
            where TException : Exception
        {
            if( key.NullReference() )
                throw Exc.Null(nameof(key));

            if( !SameMemberAsTopPartialStackTrace(e, file, member) )
                StoreFileLine(e, file, member, line);

            // we do not allow overwriting of earlier data
            string name = key;
            int i = 2;
            while( TryGetValue(e, name, out _) )
            {
                name = key + i.ToString("D", CultureInfo.InvariantCulture);
                ++i;
            }

            AddOrSet(e, name, value);
            return e;
        }

        /// <summary>
        /// Executes the delegate, and stores it's return value in the exception.
        /// Exceptions thrown by the delegate silently replace the intended value.
        /// </summary>
        /// <typeparam name="TException">The type of the exception.</typeparam>
        /// <param name="e">The exception to store data in.</param>
        /// <param name="name">The name of the value.</param>
        /// <param name="dlgt">The delegate returning the value to store. Exceptions thrown will be silently caught.</param>
        /// <param name="file">The source file that contains the caller.</param>
        /// <param name="member">The method or property name of the caller to this method.</param>
        /// <param name="line">The line number in the source file at which this method is called.</param>
        /// <returns>The exception data was stored in.</returns>
        public static TException Store<TException>(
            this TException e,
            string name,
            Func<string> dlgt,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
            where TException : Exception
        {
            string value;
            Exception exception;
            try
            {
                if( dlgt.NullReference() )
                    throw new ArgumentNullException("Delegate is null!", innerException: null);

                value = dlgt();
                exception = null;
            }
            catch( Exception ex )
            {
                value = default(string);
                exception = ex;
            }

            if( exception.NullReference() )
                return Store(e, name, value, file, member, line);
            else
                return Store(e, name, $"{exception.GetType().Name}: {exception.Message}", file, member, line);
        }

        #endregion
    }
}
