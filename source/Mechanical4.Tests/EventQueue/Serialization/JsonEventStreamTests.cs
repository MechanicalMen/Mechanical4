using System;
using System.IO;
using Mechanical4.EventQueue.Serialization;
using NUnit.Framework;

namespace Mechanical4.EventQueue.Tests.Serialization
{
    [TestFixture]
    public static class JsonEventStreamTests
    {
        [Test]
        public static void InvalidConstructorParameters()
        {
            Assert.Throws<ArgumentNullException>(() => new JsonEventStreamWriter(null, true));
            Assert.Throws<ArgumentNullException>(() => new JsonEventStreamWriter(null, false));
            Assert.Throws<ArgumentNullException>(() => new JsonEventStreamReader(null));
        }

        [Test]
        public static void GeneralTests()
        {
            IEventStreamWriter getWriter( Stream stream ) => new JsonEventStreamWriter(stream, indent: true);
            IEventStreamReader getReader( Stream stream ) => new JsonEventStreamReader(stream);

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
