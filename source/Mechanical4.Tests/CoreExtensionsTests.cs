using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

namespace Mechanical4.Tests
{
    [TestFixture]
    public static class CoreExtensionsTests
    {
        #region EnumerableWrapper

        private class EnumerableWrapper<T> : IEnumerable<T>
        {
            private readonly IList<T> list;

            internal EnumerableWrapper( IList<T> listToWrap )
            {
                if( listToWrap.NullReference() )
                    throw new ArgumentNullException(nameof(listToWrap));

                this.list = listToWrap;
            }

            public IEnumerator<T> GetEnumerator()
            {
                return list.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        #endregion

        #region ListWrapper

        private class ListWrapper<T> : IList<T>
        {
            private readonly IList<T> list;

            internal ListWrapper( IList<T> listToWrap )
            {
                if( listToWrap.NullReference() )
                    throw new ArgumentNullException(nameof(listToWrap));

                this.list = listToWrap;
            }

            public T this[int index]
            {
                get { return this.list[index]; }
                set { throw new NotImplementedException(); }
            }

            public int Count
            {
                get { return this.list.Count; }
            }

            public bool IsReadOnly
            {
                get { throw new NotImplementedException(); }
            }

            public void Add( T item )
            {
                throw new NotImplementedException();
            }

            public void Clear()
            {
                throw new NotImplementedException();
            }

            public bool Contains( T item )
            {
                throw new NotImplementedException();
            }

            public void CopyTo( T[] array, int arrayIndex )
            {
                throw new NotImplementedException();
            }

            public IEnumerator<T> GetEnumerator()
            {
                throw new NotImplementedException();
            }

            public int IndexOf( T item )
            {
                throw new NotImplementedException();
            }

            public void Insert( int index, T item )
            {
                throw new NotImplementedException();
            }

            public bool Remove( T item )
            {
                throw new NotImplementedException();
            }

            public void RemoveAt( int index )
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region ReadOnlyListWrapper

        private class ReadOnlyListWrapper<T> : IReadOnlyList<T>
        {
            private readonly IList<T> list;

            internal ReadOnlyListWrapper( IList<T> listToWrap )
            {
                if( listToWrap.NullReference() )
                    throw new ArgumentNullException(nameof(listToWrap));

                this.list = listToWrap;
            }

            public T this[int index]
            {
                get { return this.list[index]; }
            }

            public int Count
            {
                get { return this.list.Count; }
            }

            public IEnumerator<T> GetEnumerator()
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        [Test]
        public static void ObjectExtensionTests()
        {
            var obj = new object();
            Assert.AreEqual(true, obj.NotNullReference());
            Assert.AreEqual(false, obj.NullReference());

            obj = null;
            Assert.AreEqual(false, obj.NotNullReference());
            Assert.AreEqual(true, obj.NullReference());
        }

        [Test]
        public static void StringExtensionTests()
        {
            // NullOrEmpty
            Assert.True(((string)null).NullOrEmpty());
            Assert.True(string.Empty.NullOrEmpty());
            Assert.False(" a b ".NullOrEmpty());

            // NullOrLengthy
            Assert.True(((string)null).NullOrLengthy());
            Assert.True(string.Empty.NullOrLengthy());
            Assert.True(" ".NullOrLengthy());
            Assert.True(" a".NullOrLengthy());
            Assert.True("a ".NullOrLengthy());
            Assert.True(" a ".NullOrLengthy());
            Assert.False("a b".NullOrLengthy());

            // NullOrWhiteSpace
            Assert.True(((string)null).NullOrWhiteSpace());
            Assert.True(string.Empty.NullOrWhiteSpace());
            Assert.True(" ".NullOrWhiteSpace());
            Assert.False(" a ".NullOrWhiteSpace());
        }

        [Test]
        public static void CollectionExtensionTests()
        {
            var nullEnumerable = (IEnumerable<string>)null;
            var empty = new string[0];
            var notSparse = new string[] { "a", "b", "c" };
            var sparse = new string[] { "a", null };
            var numbers = new int[] { 1, 2, 3 };
            var emptyNumbers = new int[0];

            // NullOrEmpty
            Assert.True(nullEnumerable.NullOrEmpty());
            Assert.True(new ListWrapper<string>(empty).NullOrEmpty());
            Assert.True(new ReadOnlyListWrapper<string>(empty).NullOrEmpty());
            Assert.True(new EnumerableWrapper<string>(empty).NullOrEmpty());
            Assert.False(new ListWrapper<int>(numbers).NullOrEmpty());
            Assert.False(new ReadOnlyListWrapper<int>(numbers).NullOrEmpty());
            Assert.False(new EnumerableWrapper<int>(numbers).NullOrEmpty());

            // NullOrSparse
            Assert.True(nullEnumerable.NullOrSparse());
            Assert.False(new ListWrapper<string>(empty).NullOrSparse());
            Assert.False(new ReadOnlyListWrapper<string>(empty).NullOrSparse());
            Assert.False(new EnumerableWrapper<string>(empty).NullOrSparse());
            Assert.True(new ListWrapper<string>(sparse).NullOrSparse());
            Assert.True(new ReadOnlyListWrapper<string>(sparse).NullOrSparse());
            Assert.True(new EnumerableWrapper<string>(sparse).NullOrSparse());
            Assert.False(new ListWrapper<string>(notSparse).NullOrSparse());
            Assert.False(new ReadOnlyListWrapper<string>(notSparse).NullOrSparse());
            Assert.False(new EnumerableWrapper<string>(notSparse).NullOrSparse());

            // NullEmptyOrSparse
            Assert.True(nullEnumerable.NullEmptyOrSparse());
            Assert.True(new ListWrapper<string>(empty).NullEmptyOrSparse());
            Assert.True(new ReadOnlyListWrapper<string>(empty).NullEmptyOrSparse());
            Assert.True(new EnumerableWrapper<string>(empty).NullEmptyOrSparse());
            Assert.True(new ListWrapper<string>(sparse).NullEmptyOrSparse());
            Assert.True(new ReadOnlyListWrapper<string>(sparse).NullEmptyOrSparse());
            Assert.True(new EnumerableWrapper<string>(sparse).NullEmptyOrSparse());
            Assert.False(new ListWrapper<string>(notSparse).NullEmptyOrSparse());
            Assert.False(new ReadOnlyListWrapper<string>(notSparse).NullEmptyOrSparse());
            Assert.False(new EnumerableWrapper<string>(notSparse).NullEmptyOrSparse());

            // Contains(predicate)
            // NOTE: mostly tested implicitly by NullOrSparse/NullEmptyOrSparse
            Assert.Throws<ArgumentNullException>(() => CoreExtensions.Contains(sequence: (IEnumerable<int>)null, predicate: i => true));
            Assert.Throws<ArgumentNullException>(() => CoreExtensions.Contains(sequence: numbers, predicate: null));

            // FirstOrNullable()
            Assert.Throws<ArgumentNullException>(() => CoreExtensions.FirstOrNullable(sequence: (IEnumerable<int>)null));
            Assert.False(new ListWrapper<int>(emptyNumbers).FirstOrNullable().HasValue);
            Assert.False(new ReadOnlyListWrapper<int>(emptyNumbers).FirstOrNullable().HasValue);
            Assert.False(new EnumerableWrapper<int>(emptyNumbers).FirstOrNullable().HasValue);
            Assert.AreEqual(1, new ListWrapper<int>(numbers).FirstOrNullable().Value);
            Assert.AreEqual(1, new ReadOnlyListWrapper<int>(numbers).FirstOrNullable().Value);
            Assert.AreEqual(1, new EnumerableWrapper<int>(numbers).FirstOrNullable().Value);

            // FirstOrNullable(predicate)
            Assert.AreEqual(3, numbers.FirstOrNullable(i => i == 3).Value);
            Assert.False(numbers.FirstOrNullable(i => i == 4).HasValue);
        }

        [Test]
        public static void GetPairs()
        {
            // unused Data property
            var e = new Exception();
            var pairs = e.GetPairs();
            Assert.NotNull(pairs);
            Assert.AreEqual(0, pairs.Length);

            // string-string pair
            e = new Exception();
            e.Data.Add("key", "value");
            pairs = e.GetPairs();
            Assert.NotNull(pairs);
            Assert.AreEqual(1, pairs.Length);
            Assert.True(string.Equals("key", pairs[0].Key, StringComparison.Ordinal));
            Assert.True(string.Equals("value", pairs[0].Value, StringComparison.Ordinal));

            // int-string
            e = new Exception();
            e.Data.Add(5, "value");
            pairs = e.GetPairs();
            Assert.NotNull(pairs);
            Assert.AreEqual(0, pairs.Length);

            // int-string
            e = new Exception();
            e.Data.Add("key", 6);
            pairs = e.GetPairs();
            Assert.NotNull(pairs);
            Assert.AreEqual(0, pairs.Length);

            // int-int
            e = new Exception();
            e.Data.Add(5, 6);
            pairs = e.GetPairs();
            Assert.NotNull(pairs);
            Assert.AreEqual(0, pairs.Length);
        }

        [Test]
        public static void StoreFileLine()
        {
            // first call
            var e = new Exception();
            e.StoreFileLine("f", "m", 1);
            var pairs = e.GetPairs();
            Assert.True(string.Equals("PartialStackTrace", pairs[0].Key, StringComparison.Ordinal));
            Assert.True(string.Equals("m, f:1", pairs[0].Value, StringComparison.Ordinal));

            // second call (same member)
            e.StoreFileLine("f", "m", 10);
            pairs = e.GetPairs();
            Assert.True(string.Equals("PartialStackTrace", pairs[0].Key, StringComparison.Ordinal));
            Assert.True(string.Equals("m, f:10\r\nm, f:1", pairs[0].Value, StringComparison.Ordinal));
        }

        [Test]
        public static void Store_String()
        {
            string get( Exception exception, string key )
            {
                var dictionary = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach( var pair in exception.GetPairs() )
                    dictionary.Add(pair.Key, pair.Value);
                return dictionary[key];
            }

            // first Store adds two keys
            var e = new Exception();
            e.Store("key", "value", "f", "m", 1);
            var pairs = e.GetPairs();
            Assert.AreEqual(2, pairs.Length);
            Assert.True(string.Equals("value", get(e, "key"), StringComparison.Ordinal));
            Assert.True(string.Equals("m, f:1", get(e, "PartialStackTrace"), StringComparison.Ordinal));

            // second Store, with a different key, adds only one new key
            e.Store("Key", "Value");
            pairs = e.GetPairs();
            Assert.AreEqual(3, pairs.Length);
            Assert.True(string.Equals("Value", get(e, "Key"), StringComparison.Ordinal));

            // 2x Store, same key --> second key automatically changed
            e = new Exception();
            e.Store("key", "value");
            e.Store("key", "value2");
            pairs = e.GetPairs();
            Assert.AreEqual(3, pairs.Length);
            Assert.True(string.Equals("value", get(e, "key"), StringComparison.Ordinal));
            Assert.True(string.Equals("value2", get(e, "key2"), StringComparison.Ordinal));

            // 2x Store, same member --> single line PartialStackTrace
            e = new Exception();
            e.Store("key", "value", "f", "m", 1);
            e.Store("key", "value", "f", "m", 2);
            Assert.True(string.Equals("m, f:1", get(e, "PartialStackTrace"), StringComparison.Ordinal));

            // 2x Store, different member (file name) --> multi line PartialStackTrace
            e = new Exception();
            e.Store("key", "value", "f", "m", 1);
            e.Store("key", "value", "X", "m", 1);
            Assert.True(string.Equals("m, X:1\r\nm, f:1", get(e, "PartialStackTrace"), StringComparison.Ordinal));

            // 2x Store, different member (member name) --> multi line PartialStackTrace
            e = new Exception();
            e.Store("key", "value", "f", "m", 1);
            e.Store("key", "value", "f", "X", 1);
            Assert.True(string.Equals("X, f:1\r\nm, f:1", get(e, "PartialStackTrace"), StringComparison.Ordinal));

            // null key
            e = new Exception();
            Assert.Throws<ArgumentNullException>(() => e.Store(null, "value"));

            // null value
            e.Store("key", (string)null);
            Assert.Null(null, get(e, "key"));


            // delegate Store
            e = new Exception();
            e.Store("key", () => "value");
            Assert.True(string.Equals("value", get(e, "key"), StringComparison.Ordinal));

            // null delegate
            e = new Exception();
            e.Store("key", (Func<string>)null);
            Assert.True(get(e, "key").Contains("ArgumentNullException"));

            // exception delegate
            e = new Exception();
            e.Store("key", () => throw new UriFormatException());
            Assert.True(get(e, "key").Contains("UriFormatException"));
        }
    }
}
