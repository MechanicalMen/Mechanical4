using System;
using Mechanical4.Misc;
using NUnit.Framework;

namespace Mechanical4.Tests.Misc
{
    [TestFixture]
    public static class ReferenceEqualityComparerTests
    {
        [Test]
        public static void Equals()
        {
            // null equality
            Assert.True(ReferenceEqualityComparer<string>.Default.Equals(null, null));
            Assert.False(ReferenceEqualityComparer<string>.Default.Equals(null, string.Empty));
            Assert.False(ReferenceEqualityComparer<string>.Default.Equals(null, "a"));
            Assert.False(ReferenceEqualityComparer<string>.Default.Equals("a", null));

            // non-null equality
            Assert.False(ReferenceEqualityComparer<string>.Default.Equals(string.Empty, "a"));
            Assert.False(ReferenceEqualityComparer<int[]>.Default.Equals(new int[0], new int[0]));
        }

        [Test]
        public static void GetHashCode()
        {
            Assert.Throws<NotSupportedException>(() => ReferenceEqualityComparer<string>.Default.GetHashCode(null));
            Assert.Throws<NotSupportedException>(() => ReferenceEqualityComparer<string>.Default.GetHashCode("a"));
        }
    }
}
