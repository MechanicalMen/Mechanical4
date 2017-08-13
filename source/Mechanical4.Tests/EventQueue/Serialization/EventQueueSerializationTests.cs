using System;
using System.IO;
using System.Threading;
using Mechanical4.EventQueue.Serialization;
using NUnit.Framework;

namespace Mechanical4.EventQueue.Tests.Serialization
{
    [TestFixture]
    public static class EventQueueSerializationTests
    {
        #region Event Types

        private class AllTypesEvent : SerializableEventBase
        {
            public AllTypesEvent()
            {
            }

            public byte UInt8 { get; set; }
            public int Int32 { get; set; }
            public long Int64 { get; set; }
            public float Single { get; set; }
            public string String { get; set; }
            public bool Boolean { get; set; }

            public AllTypesEvent( IEventReader reader )
            {
                this.UInt8 = reader.ReadUInt8();
                this.Int32 = reader.ReadInt32();
                this.Int64 = reader.ReadInt64();
                this.Single = reader.ReadSingle();
                this.String = reader.ReadString();
                this.Boolean = reader.ReadBoolean();
            }

            public override void Serialize( IEventWriter writer )
            {
                writer.Write(this.UInt8);
                writer.Write(this.Int32);
                writer.Write(this.Int64);
                writer.Write(this.Single);
                writer.Write(this.String);
                writer.Write(this.Boolean);
            }
        }

        private class TooMuchReadingEvent : SerializableEventBase
        {
            public TooMuchReadingEvent()
            {
            }

            public int Integer1 { get; set; }
            public int Integer2 { get; set; }

            public TooMuchReadingEvent( IEventReader reader )
            {
                this.Integer1 = reader.ReadInt32();
                Assert.False(reader.IsAtEnd); // tests IsAtEnd as well
                this.Integer2 = reader.ReadInt32();
                Assert.True(reader.IsAtEnd);
                reader.ReadInt32(); // nothing to read at this point!
            }

            public override void Serialize( IEventWriter writer )
            {
                writer.Write(this.Integer1);
                writer.Write(this.Integer2);
            }
        }

        private class TooLittleReadingEvent : SerializableEventBase
        {
            public TooLittleReadingEvent()
            {
            }

            public int Integer1 { get; set; }
            public int Integer2 { get; set; }

            public TooLittleReadingEvent( IEventReader reader )
            {
                this.Integer1 = reader.ReadInt32();
            }

            public override void Serialize( IEventWriter writer )
            {
                writer.Write(this.Integer1);
                writer.Write(this.Integer2);
            }
        }

        private class EmptyEvent : SerializableEventBase
        {
            public EmptyEvent()
            {
            }

            public EmptyEvent( IEventReader reader )
            {
            }

            public override void Serialize( IEventWriter writer )
            {
            }
        }

        #endregion

        internal static void AllTypesRoundTrip( Func<Stream, IEventStreamWriter> getWriter, Func<Stream, IEventStreamReader> getReader )
        {
            var eventToWrite = new AllTypesEvent()
            {
                UInt8 = 3,
                Int32 = 5,
                Int64 = int.MaxValue + 1L,
                Single = (float)Math.PI,
                String = "test",
                Boolean = !default(bool)
            };

            var outputStream = new MemoryStream();
            using( var serializer = new EventQueueSerializer(getWriter(outputStream)) )
            {
                serializer.Serialize(eventToWrite);
                serializer.Serialize(new AllTypesEvent());
            }

            var inputStream = new MemoryStream(outputStream.ToArray());
            using( var deserializer = new EventQueueDeserializer(getReader(inputStream)) )
            {
                var eventRead = deserializer.Read();
                Assert.NotNull(eventRead);
                var dummyEvent = (AllTypesEvent)eventRead;
                Assert.AreEqual(eventToWrite.UInt8, dummyEvent.UInt8);
                Assert.AreEqual(eventToWrite.Int32, dummyEvent.Int32);
                Assert.AreEqual(eventToWrite.Int64, dummyEvent.Int64);
                Assert.AreEqual(eventToWrite.Single, dummyEvent.Single);
                Assert.True(string.Equals(eventToWrite.String, dummyEvent.String, StringComparison.Ordinal));
                Assert.AreEqual(eventToWrite.Boolean, dummyEvent.Boolean);

                eventRead = deserializer.Read();
                Assert.NotNull(eventRead);
                dummyEvent = (AllTypesEvent)eventRead;
                Assert.AreEqual(0, dummyEvent.UInt8);
                Assert.AreEqual(0, dummyEvent.Int32);
                Assert.AreEqual(0, dummyEvent.Int64);
                Assert.AreEqual(0, dummyEvent.Single);
                Assert.AreSame(null, dummyEvent.String);
                Assert.AreEqual(false, dummyEvent.Boolean);

                Assert.Null(deserializer.Read());
                Assert.Null(deserializer.Read());
            }
        }

        internal static void TooLittleReading( Func<Stream, IEventStreamWriter> getWriter, Func<Stream, IEventStreamReader> getReader )
        {
            var eventToWrite = new TooLittleReadingEvent()
            {
                Integer1 = 1,
                Integer2 = 2
            };

            var outputStream = new MemoryStream();
            using( var serializer = new EventQueueSerializer(getWriter(outputStream)) )
            {
                serializer.Serialize(eventToWrite);
                serializer.Serialize(new AllTypesEvent() { Int32 = 7 });
            }

            var inputStream = new MemoryStream(outputStream.ToArray());
            using( var deserializer = new EventQueueDeserializer(getReader(inputStream)) )
            {
                var eventRead = deserializer.Read(); // no problem deserializing
                Assert.NotNull(eventRead);
                var evnt = (TooLittleReadingEvent)eventRead;
                Assert.AreEqual(eventToWrite.Integer1, evnt.Integer1);
                Assert.AreEqual(default(int), evnt.Integer2); // obviously uninitialized

                var evnt2 = (AllTypesEvent)deserializer.Read(); // next reading succeeds without a problem too
                Assert.AreEqual(7, evnt2.Int32);

                Assert.Null(deserializer.Read());
                Assert.Null(deserializer.Read());
            }
        }

        internal static void TooMuchReading( Func<Stream, IEventStreamWriter> getWriter, Func<Stream, IEventStreamReader> getReader )
        {
            var eventToWrite = new TooMuchReadingEvent()
            {
                Integer1 = 1,
                Integer2 = 2
            };

            var outputStream = new MemoryStream();
            using( var serializer = new EventQueueSerializer(getWriter(outputStream)) )
            {
                serializer.Serialize(eventToWrite);
                serializer.Serialize(new AllTypesEvent()); // it should fail, even if it is not the last event in the stream
            }

            var inputStream = new MemoryStream(outputStream.ToArray());
            using( var deserializer = new EventQueueDeserializer(getReader(inputStream)) )
                Assert.Throws<FormatException>(() => deserializer.Read());
        }

        internal static void BasicPropertiesPreserved( Func<Stream, IEventStreamWriter> getWriter, Func<Stream, IEventStreamReader> getReader )
        {
            var eventToWrite = new AllTypesEvent() { Int32 = 11 };

            var queue = new ManualEventQueue();
            queue.Enqueue(eventToWrite);
            queue.HandleNext();

            var outputStream = new MemoryStream();
            using( var serializer = new EventQueueSerializer(getWriter(outputStream)) )
                serializer.Serialize(eventToWrite);

            Thread.Sleep(TimeSpan.FromMilliseconds(100)); // verify that the deserialized timestamp is not the current time

            var inputStream = new MemoryStream(outputStream.ToArray());
            using( var deserializer = new EventQueueDeserializer(getReader(inputStream)) )
            {
                var eventRead = (AllTypesEvent)deserializer.Read();
                Assert.True(string.Equals(eventToWrite.EventEnqueuePos, eventRead.EventEnqueuePos, StringComparison.Ordinal));
                Assert.AreEqual(eventToWrite.EventEnqueueTime.Ticks, eventRead.EventEnqueueTime.Ticks);
                Assert.AreEqual(eventToWrite.EventEnqueueTime.Kind, eventRead.EventEnqueueTime.Kind);
            }
        }

        internal static void DisposedAccess( Func<Stream, IEventStreamWriter> getWriter, Func<Stream, IEventStreamReader> getReader )
        {
            var eventToWrite = new TooMuchReadingEvent();

            var outputStream = new MemoryStream();
            var writer = getWriter(outputStream);
            var serializer = new EventQueueSerializer(writer);
            serializer.Dispose();
            Assert.Throws<ObjectDisposedException>(() => writer.BeginNewEvent());
            Assert.Throws<ObjectDisposedException>(() => writer.EndLastEvent());
            Assert.Throws<ObjectDisposedException>(() => serializer.Serialize(new AllTypesEvent()));
            Assert.Throws<ObjectDisposedException>(() => serializer.SetWriter(null));

            //// NOTE: an "empty" stream should be perfectly readable

            var inputStream = new MemoryStream(outputStream.ToArray());
            var reader = getReader(inputStream);
            var deserializer = new EventQueueDeserializer(reader);
            deserializer.Dispose();
            Assert.Throws<ObjectDisposedException>(() => reader.TryRead());
            Assert.Throws<ObjectDisposedException>(() => deserializer.Read());
        }

        internal static void EmptyEventSerialization( Func<Stream, IEventStreamWriter> getWriter, Func<Stream, IEventStreamReader> getReader )
        {
            var outputStream = new MemoryStream();
            using( var serializer = new EventQueueSerializer(getWriter(outputStream)) )
            {
                serializer.Serialize(new EmptyEvent());
                serializer.Serialize(new EmptyEvent());
            }

            var inputStream = new MemoryStream(outputStream.ToArray());
            using( var deserializer = new EventQueueDeserializer(getReader(inputStream)) )
            {
                var evnt1 = deserializer.Read();
                var evnt2 = deserializer.Read();
                Assert.True(evnt1 is EmptyEvent);
                Assert.True(evnt2 is EmptyEvent);
                Assert.AreNotSame(evnt1, evnt2);
            }
        }

        internal static void EmptyStreamSerialization( Func<Stream, IEventStreamWriter> getWriter, Func<Stream, IEventStreamReader> getReader )
        {
            var outputStream = new MemoryStream();
            using( var serializer = new EventQueueSerializer(getWriter(outputStream)) )
            {
            }

            var inputStream = new MemoryStream(outputStream.ToArray());
            using( var deserializer = new EventQueueDeserializer(getReader(inputStream)) )
            {
                Assert.Null(deserializer.Read());
            }
        }

        [Test]
        public static void InvalidEventQueueSerializationConstructorParams()
        {
            Assert.Throws<ArgumentNullException>(() => new EventQueueDeserializer(null));

            using( var serializer = new EventQueueSerializer(null) )
            {
            }
        }
    }
}
