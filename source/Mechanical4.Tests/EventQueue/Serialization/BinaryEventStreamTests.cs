using System;
using System.IO;
using Mechanical4.EventQueue.Serialization;
using NUnit.Framework;

namespace Mechanical4.EventQueue.Tests.Serialization
{
    [TestFixture]
    public static class BinaryEventStreamTests
    {
        [Test]
        public static void InvalidConstructorParameters()
        {
            Assert.Throws<ArgumentNullException>(() => new BinaryEventStreamWriter(null));
            Assert.Throws<ArgumentNullException>(() => new BinaryEventStreamReader(null));
        }

        [Test]
        public static void GeneralTests()
        {
            IEventStreamWriter getWriter( Stream stream ) => new BinaryEventStreamWriter(stream);
            IEventStreamReader getReader( Stream stream ) => new BinaryEventStreamReader(stream);

            EventQueueSerializationTests.AllTypesRoundTrip(getWriter, getReader);
            EventQueueSerializationTests.TooLittleReading(getWriter, getReader);
            EventQueueSerializationTests.TooMuchReading(getWriter, getReader);
            EventQueueSerializationTests.BasicPropertiesPreserved(getWriter, getReader);
            EventQueueSerializationTests.DisposedAccess(getWriter, getReader);
            EventQueueSerializationTests.EmptyEventSerialization(getWriter, getReader);
            EventQueueSerializationTests.EmptyStreamSerialization(getWriter, getReader);
        }
    }
}
