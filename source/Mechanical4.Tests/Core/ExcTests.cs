using System;
using Mechanical4.Core;
using NUnit.Framework;

namespace Mechanical4.Tests.Core
{
    [TestFixture]
    public static class ExcTests
    {
        [Test]
        public static void Null()
        {
            Assert.NotNull(Exc.Null("test"));
            Assert.NotNull(Exc.Null(string.Empty));
            Assert.NotNull(Exc.Null(null));

            const string guid = "d5b5b296e9dd4f60a164e92deceeb530";
            var exception = Exc.Null(guid);
            Assert.IsInstanceOf<ArgumentNullException>(exception);
            Assert.True(exception.Message.Contains(guid));
        }
    }
}
