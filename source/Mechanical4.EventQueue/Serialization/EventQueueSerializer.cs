using System;
using System.Collections.Generic;
using System.Globalization;
using Mechanical4.Core;

namespace Mechanical4.EventQueue.Serialization
{
    /// <summary>
    /// Serializes events using an <see cref="IEventStreamWriter"/>.
    /// Not thread-safe.
    /// </summary>
    public class EventQueueSerializer : Disposable.NonBlockingBase
    {
        #region Private Fields

        internal const int FormatVersion = 1;

        private readonly Dictionary<Type, int> typesByID = new Dictionary<Type, int>();
        private readonly Dictionary<string, int> sourcePositionsByID = new Dictionary<string, int>(StringComparer.Ordinal);
        private int nextTypeID = 0;
        private int nextPosID = 1; // 0 is reserved for null
        private IEventStreamWriter streamWriter;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="EventQueueSerializer"/> class.
        /// </summary>
        /// <param name="writer">The <see cref="IEventStreamWriter"/> to write to; or <c>null</c> to specify that later using <see cref="SetWriter"/>.</param>
        public EventQueueSerializer( IEventStreamWriter writer = null )
        {
            this.SetWriter(writer);
        }

        #endregion

        #region Disposable

        /// <summary>
        /// Releases managed (IDisposable) resources.
        /// </summary>
        protected override void OnDispose()
        {
            this.streamWriter?.Dispose();
        }

        #endregion

        #region Private Methods

        private void WriteEvent( SerializableEventBase evnt )
        {
            // start new event
            var eventWriter = this.streamWriter.BeginNewEvent();

            // write type identifier
            var type = evnt.GetType();
            if( !this.streamWriter.IsCompactFormat )
            {
                eventWriter.Write(type.AssemblyQualifiedName);
            }
            else
            {
                bool knownType = this.typesByID.TryGetValue(type, out int typeID);
                if( knownType )
                {
                    Write7BitEncoded(eventWriter, typeID);
                }
                else
                {
                    typeID = this.nextTypeID;
                    ++this.nextTypeID;
                    this.typesByID.Add(type, typeID);
                    Write7BitEncoded(eventWriter, typeID);
                    eventWriter.Write(type.AssemblyQualifiedName);
                }
            }

            // write source position
            if( !this.streamWriter.IsCompactFormat )
            {
                eventWriter.Write(evnt.EventEnqueuePos);
            }
            else
            {
                bool knownPos;
                int posID;
                if( evnt.EventEnqueuePos.NullReference() )
                {
                    knownPos = true;
                    posID = 0;
                }
                else
                {
                    knownPos = this.sourcePositionsByID.TryGetValue(evnt.EventEnqueuePos, out posID);
                }

                if( knownPos )
                {
                    Write7BitEncoded(eventWriter, posID);
                }
                else
                {
                    posID = this.nextPosID;
                    ++this.nextPosID;
                    this.sourcePositionsByID.Add(evnt.EventEnqueuePos, posID);
                    Write7BitEncoded(eventWriter, posID);
                    eventWriter.Write(evnt.EventEnqueuePos);
                }
            }

            // write timestamp
            if( !this.streamWriter.IsCompactFormat )
                eventWriter.Write(evnt.EventEnqueueTime.ToString("o", CultureInfo.InvariantCulture));
            else
                eventWriter.Write(evnt.EventEnqueueTime.Ticks);

            // write other event components
            evnt.Serialize(eventWriter);

            // stop writing
            this.streamWriter.EndLastEvent();
        }

        private static void Write7BitEncoded( IEventWriter writer, int value )
        {
            uint valueToEncode = VariableLengthUInt.ToUnsigned(value);
            byte encodedByte;
            while( true )
            {
                bool hasMore = VariableLengthUInt.EncodeNextByte(ref valueToEncode, out encodedByte);
                writer.Write(encodedByte);
                if( !hasMore )
                    break;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Updates the current <see cref="IEventStreamWriter"/> in use.
        /// Disposes of the old writer, before changing to the new one.
        /// </summary>
        /// <param name="newWriter">The new <see cref="IEventStreamWriter"/> to take ownership of.</param>
        public void SetWriter( IEventStreamWriter newWriter )
        {
            this.ThrowIfDisposed();

            // dispose of old writer
            var oldWriter = this.streamWriter;
            if( oldWriter.NotNullReference() )
                oldWriter.Dispose();

            // set new writer
            this.streamWriter = newWriter;
            this.nextTypeID = 0;
            this.typesByID.Clear();

            // make sure format version is written first
            if( this.streamWriter.NotNullReference() )
            {
                var eventWriter = this.streamWriter.BeginNewEvent();
                eventWriter.Write(FormatVersion);
                this.streamWriter.EndLastEvent();
            }
        }

        /// <summary>
        /// Serializes the specified event.
        /// </summary>
        /// <param name="evnt">The event to serialize.</param>
        public void Serialize( SerializableEventBase evnt )
        {
            if( evnt.NullReference() )
                throw Exc.Null(nameof(evnt));

            this.ThrowIfDisposed();

            this.WriteEvent(evnt);
        }

        #endregion
    }
}
