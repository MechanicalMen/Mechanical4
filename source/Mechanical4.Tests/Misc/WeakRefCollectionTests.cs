using System;
using Mechanical4.Misc;
using NUnit.Framework;

namespace Mechanical4.Tests.Misc
{
    [TestFixture]
    public static class WeakRefCollectionTests
    {
        #region AlwaysEqual

        private class AlwaysEqual : IEquatable<AlwaysEqual>
        {
            public bool Equals( AlwaysEqual other ) => true;
            public override bool Equals( object obj ) => true;
            public override int GetHashCode() => 0;
        }

        #endregion

        [Test]
        public static void Add()
        {
            var collection = new WeakRefCollection<int[]>();

            var array = new int[0];
            collection.Add(new WeakReference<int[]>(array));
            collection.Add(array); // can store multiple weak references to the same object

            Assert.Throws<ArgumentNullException>(() => collection.Add((WeakReference<int[]>)null));
            Assert.Throws<ArgumentNullException>(() => collection.Add((int[])null));
        }

        [Test]
        public static void Contains()
        {
            var strings = new WeakRefCollection<string>();
            strings.Add("a");

            // comparer is used as expected
            Assert.True(strings.Contains("a", StringComparer.Ordinal));
            Assert.False(strings.Contains("A", StringComparer.Ordinal));
            Assert.True(strings.Contains("A", StringComparer.OrdinalIgnoreCase));

            // null check
            Assert.False(strings.Contains(null)); // obvious, since you can't add null

            // default comparer (EqualityComparer<T>.Default)
            var collection = new WeakRefCollection<AlwaysEqual>();
            var item1 = new AlwaysEqual();
            var item2 = new AlwaysEqual();
            collection.Add(item1);
            Assert.True(collection.Contains(item1));
            Assert.True(collection.Contains(item2));
            Assert.True(collection.Contains(item1, ReferenceEqualityComparer<AlwaysEqual>.Default));
            Assert.False(collection.Contains(item2, ReferenceEqualityComparer<AlwaysEqual>.Default));
        }

        [Test]
        public static void Remove()
        {
            var strings = new WeakRefCollection<string>();
            strings.Add("a");
            Assert.True(strings.Remove("a"));
            Assert.False(strings.Remove("a"));

            // comparer parameter is actually used
            strings.Add("b");
            Assert.True(strings.Remove("B", StringComparer.OrdinalIgnoreCase));
        }
    }
}
