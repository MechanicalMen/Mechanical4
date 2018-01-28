using System;
using System.IO;
using Mechanical4.Core;

namespace Mechanical4.EventQueue.Serialization
{
    /// <summary>
    /// Uses a <see cref="BinaryReader"/> to deserialize events.
    /// </summary>
    public class BinaryEventStreamReader : Disposable.NonBlockingBase, IEventStreamReader, IEventReader
    {
        #region Private Fields

        private readonly Stream eventStream;
        private BinaryReader eventStreamReader;
        private long currentEventStartPosition = long.MinValue;
        private long nextEventStartPosition = long.MinValue;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryEventStreamReader"/> class.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to deserialize events from.</param>
        public BinaryEventStreamReader( Stream stream )
        {
            if( stream.NullReference() )
                throw Exc.Null(nameof(stream));

            if( !stream.CanSeek )
                throw new ArgumentException("The stream must be seekable!");

            this.eventStream = stream;
            this.eventStreamReader = new BinaryReader(this.eventStream);

            // verify format
            var formatVersion = this.eventStreamReader.ReadByte();
            if( formatVersion != BinaryEventStreamWriter.BinaryFormatVersion )
                throw new FormatException($"Unknown binary format!").Store("expectedVersion", BinaryEventStreamWriter.BinaryFormatVersion.ToString()).Store("actualVersion", formatVersion.ToString());
        }

        #endregion

        #region Disposable

        /// <summary>
        /// Releases managed (IDisposable) resources.
        /// </summary>
        protected override void OnDispose()
        {
            this.eventStreamReader.Dispose();
        }

        #endregion

        #region IEventStreamReader

        /// <summary>
        /// Gets a value indicating whether this instance expects to read size optimized content.
        /// </summary>
        public bool IsCompactFormat => true;

        /// <summary>
        /// Tries to read the next event from the stream.
        /// </summary>
        /// <returns>The <see cref="IEventReader"/> of the next event; or <c>null</c> if there are no more events</returns>
        public IEventReader TryRead()
        {
            this.ThrowIfDisposed();

            // previous event not fully read --> move to next event
            if( this.eventStream.Position < this.nextEventStartPosition )
                this.eventStream.Position = this.nextEventStartPosition;

            // end of stream reached?
            if( this.eventStream.Position == this.eventStream.Length )
                return null;

            // read new event size
            int payloadSizeInBytes = this.eventStreamReader.ReadInt32();
            this.currentEventStartPosition = this.eventStream.Position; // payloadSizeInBytes (4 bytes) is not part of the "size" we just read
            this.nextEventStartPosition = this.currentEventStartPosition + payloadSizeInBytes;

            return this;
        }

        #endregion

        #region IEventReader

        /// <summary>
        /// Gets a value indicating whether there are no more components of the event left to read.
        /// </summary>
        public bool IsAtEnd
        {
            get
            {
                this.ThrowIfDisposed();

                return this.eventStream.Position == this.nextEventStartPosition;
            }
        }

        /// <summary>
        /// Deserializes the next component of the event, as a(n) <see cref="byte"/> value.
        /// Behaviour is undetermined, if this is not the same type of value that was originally serialized.
        /// </summary>
        /// <returns>The deserialized value.</returns>
        public byte ReadUInt8()
        {
            this.ThrowIfDisposed();

            var result = this.eventStreamReader.ReadByte();
            if( this.eventStream.Position > this.nextEventStartPosition )
                throw new FormatException("Tried to read more data than was available!");
            return result;
        }

        /// <summary>
        /// Deserializes the next component of the event, as a(n) <see cref="int"/> value.
        /// Behaviour is undetermined, if this is not the same type of value that was originally serialized.
        /// </summary>
        /// <returns>The deserialized value.</returns>
        public int ReadInt32()
        {
            this.ThrowIfDisposed();

            var result = this.eventStreamReader.ReadInt32();
            if( this.eventStream.Position > this.nextEventStartPosition )
                throw new FormatException("Tried to read more data than was available!");
            return result;
        }

        /// <summary>
        /// Deserializes the next component of the event, as a(n) <see cref="long"/> value.
        /// Behaviour is undetermined, if this is not the same type of value that was originally serialized.
        /// </summary>
        /// <returns>The deserialized value.</returns>
        public long ReadInt64()
        {
            this.ThrowIfDisposed();

            var result = this.eventStreamReader.ReadInt64();
            if( this.eventStream.Position > this.nextEventStartPosition )
                throw new FormatException("Tried to read more data than was available!");
            return result;
        }

        /// <summary>
        /// Deserializes the next component of the event, as a(n) <see cref="float"/> value.
        /// Behaviour is undetermined, if this is not the same type of value that was originally serialized.
        /// </summary>
        /// <returns>The deserialized value.</returns>
        public float ReadSingle()
        {
            this.ThrowIfDisposed();

            var result = this.eventStreamReader.ReadSingle();
            if( this.eventStream.Position > this.nextEventStartPosition )
                throw new FormatException("Tried to read more data than was available!");
            return result;
        }

        /// <summary>
        /// Deserializes the next component of the event, as a(n) <see cref="string"/> value.
        /// Behaviour is undetermined, if this is not the same type of value that was originally serialized.
        /// </summary>
        /// <returns>The deserialized value.</returns>
        public string ReadString()
        {
            this.ThrowIfDisposed();

            bool notNull = this.eventStreamReader.ReadBoolean();
            string result = null;
            if( notNull )
                result = this.eventStreamReader.ReadString();
            if( this.eventStream.Position > this.nextEventStartPosition )
                throw new FormatException("Tried to read more data than was available!");
            return result;
        }

        /// <summary>
        /// Deserializes the next component of the event, as a(n) <see cref="bool"/> value.
        /// Behaviour is undetermined, if this is not the same type of value that was originally serialized.
        /// </summary>
        /// <returns>The deserialized value.</returns>
        public bool ReadBoolean()
        {
            this.ThrowIfDisposed();

            var result = this.eventStreamReader.ReadBoolean();
            if( this.eventStream.Position > this.nextEventStartPosition )
                throw new FormatException("Tried to read more data than was available!");
            return result;
        }

        #endregion
    }
}
