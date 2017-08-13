using System.IO;
using System.Text;
using Mechanical4.Core;
using Newtonsoft.Json;

namespace Mechanical4.EventQueue.Serialization
{
    /// <summary>
    /// Serialize events into JSON format.
    /// </summary>
    public class JsonEventStreamWriter : Disposable.NonBlockingBase, IEventStreamWriter, IEventWriter
    {
        #region Private Fields

        internal const byte JsonFormatVersion = 1;
        internal const string FormatVersionPropertyName = "jsonFormatVersion";
        internal const string EventsPropertyName = "events";

        private readonly JsonTextWriter writer;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonEventStreamWriter"/> class.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to write events to.</param>
        /// <param name="indent">Determines whether to indent the generated JSON.</param>
        public JsonEventStreamWriter( Stream stream, bool indent )
        {
            if( stream.NullReference() )
                throw Exc.Null(nameof(stream));

            this.writer = new JsonTextWriter(new StreamWriter(stream, Encoding.UTF8));
            this.writer.Formatting = indent ? Formatting.Indented : Formatting.None;

            this.writer.WriteStartObject();
            this.writer.WritePropertyName(FormatVersionPropertyName);
            this.writer.WriteValue(JsonFormatVersion);
            this.writer.WritePropertyName(EventsPropertyName);
            this.writer.WriteStartArray();
        }

        #endregion

        #region Disposable

        /// <summary>
        /// Releases managed (IDisposable) resources.
        /// </summary>
        protected override void OnDispose()
        {
            this.writer.WriteEndArray();
            this.writer.WriteEndObject();

            this.writer.Close();
        }

        #endregion

        #region IEventStreamWriter

        /// <summary>
        /// Gets a value indicating whether this instance expects to write size optimized content.
        /// </summary>
        public bool IsCompactFormat => false;

        /// <summary>
        /// Starts writing a new event.
        /// </summary>
        /// <returns>The <see cref="IEventWriter"/> to use.</returns>
        public IEventWriter BeginNewEvent()
        {
            this.ThrowIfDisposed();

            this.writer.WriteStartArray();
            return this;
        }

        /// <summary>
        /// Stops writing of the last event.
        /// </summary>
        public void EndLastEvent()
        {
            this.ThrowIfDisposed();

            this.writer.WriteEndArray();
        }

        #endregion

        #region IEventWriter

        /// <summary>
        /// Serializes the specified value.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        public void Write( byte value )
        {
            this.ThrowIfDisposed();

            this.writer.WriteValue(value);
        }

        /// <summary>
        /// Serializes the specified value.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        public void Write( int value )
        {
            this.ThrowIfDisposed();

            this.writer.WriteValue(value);
        }

        /// <summary>
        /// Serializes the specified value.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        public void Write( long value )
        {
            this.ThrowIfDisposed();

            this.writer.WriteValue(value);
        }

        /// <summary>
        /// Serializes the specified value.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        public void Write( string value )
        {
            this.ThrowIfDisposed();

            this.writer.WriteValue(value);
        }

        /// <summary>
        /// Serializes the specified value.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        public void Write( bool value )
        {
            this.ThrowIfDisposed();

            this.writer.WriteValue(value);
        }

        #endregion
    }
}
