using System.IO;
using Mechanical4.Core;

namespace Mechanical4.EventQueue.Serialization
{
    /// <summary>
    /// Uses a <see cref="BinaryWriter"/> to serialize events.
    /// </summary>
    public class BinaryEventStreamWriter : Disposable.NonBlockingBase, IEventStreamWriter, IEventWriter
    {
        #region Private Fields

        internal const byte BinaryFormatVersion = 1;

        private readonly Stream eventStream;
        private readonly MemoryStream memoryStream;
        private readonly BinaryWriter eventStreamWriter;
        private readonly BinaryWriter memoryStreamWriter;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryEventStreamWriter"/> class.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to write events to.</param>
        public BinaryEventStreamWriter( Stream stream )
        {
            // initialize fields
            this.eventStream = stream ?? throw Exc.Null(nameof(stream));
            this.memoryStream = new MemoryStream();
            this.eventStreamWriter = new BinaryWriter(this.eventStream); // BinaryWriter uses UTF8 without BOM, by default
            this.memoryStreamWriter = new BinaryWriter(this.memoryStream);

            // indicate the format version at the start of the stream
            this.eventStreamWriter.Write(BinaryFormatVersion);
        }

        #endregion

        #region Disposable

        /// <summary>
        /// Releases managed (IDisposable) resources.
        /// </summary>
        protected override void OnDispose()
        {
            this.eventStreamWriter.Dispose(); // BinaryWriter disposes of the Stream it writes to
            this.memoryStreamWriter.Dispose();
        }

        #endregion

        #region IEventStreamWriter

        /// <summary>
        /// Gets a value indicating whether this instance expects to write size optimized content.
        /// </summary>
        public bool IsCompactFormat => true;

        /// <summary>
        /// Starts writing a new event.
        /// </summary>
        /// <returns>The <see cref="IEventWriter"/> to use.</returns>
        public IEventWriter BeginNewEvent()
        {
            this.ThrowIfDisposed();

            return this;
        }

        /// <summary>
        /// Stops writing of the last event.
        /// </summary>
        public void EndLastEvent()
        {
            this.ThrowIfDisposed();

            // write size of event data, in bytes
            int payloadSizeInBytes;
            checked
            {
                payloadSizeInBytes = (int)this.memoryStream.Length;
            }
            this.eventStreamWriter.Write(payloadSizeInBytes);

            // write actual event data
            this.eventStreamWriter.Flush();
            this.memoryStreamWriter.Flush();
            this.memoryStream.Position = 0;
            this.memoryStream.CopyTo(this.eventStream);

            // reset for next event
            this.memoryStream.SetLength(0);
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

            this.memoryStreamWriter.Write(value);
        }

        /// <summary>
        /// Serializes the specified value.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        public void Write( int value )
        {
            this.ThrowIfDisposed();

            this.memoryStreamWriter.Write(value);
        }

        /// <summary>
        /// Serializes the specified value.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        public void Write( long value )
        {
            this.ThrowIfDisposed();

            this.memoryStreamWriter.Write(value);
        }

        /// <summary>
        /// Serializes the specified value.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        public void Write( string value )
        {
            this.ThrowIfDisposed();

            if( value.NotNullReference() )
            {
                this.memoryStreamWriter.Write(true);
                this.memoryStreamWriter.Write(value);
            }
            else
            {
                this.memoryStreamWriter.Write(false);
            }
        }

        /// <summary>
        /// Serializes the specified value.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        public void Write( bool value )
        {
            this.ThrowIfDisposed();

            this.memoryStreamWriter.Write(value);
        }

        #endregion
    }
}
