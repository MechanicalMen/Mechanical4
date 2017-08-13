using System;
using System.IO;
using System.Text;
using Mechanical4.Core;
using Newtonsoft.Json;

namespace Mechanical4.EventQueue.Serialization
{
    /// <summary>
    /// Deserialize events from JSON format.
    /// </summary>
    public class JsonEventStreamReader : Disposable.NonBlockingBase, IEventStreamReader, IEventReader
    {
        #region Private Fields

        private readonly JsonTextReader reader;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonEventStreamReader"/> class.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to deserialize events from.</param>
        public JsonEventStreamReader( Stream stream )
        {
            if( stream.NullReference() )
                throw Exc.Null(nameof(stream));

            this.reader = new JsonTextReader(new StreamReader(stream, Encoding.UTF8));
            this.reader.DateParseHandling = DateParseHandling.None;

            this.ReadToken(JsonToken.StartObject);

            this.ReadPropertyName(JsonEventStreamWriter.FormatVersionPropertyName);
            this.AssertCanRead();
            if( this.reader.TokenType != JsonToken.Integer
             || (long)this.reader.Value != JsonEventStreamWriter.JsonFormatVersion )
                throw new FormatException($"Unknown json format! (expected version: {JsonEventStreamWriter.JsonFormatVersion}; actual version (as string): \"{this.reader.Value?.ToString()}\")");

            this.ReadPropertyName(JsonEventStreamWriter.EventsPropertyName);
            this.ReadToken(JsonToken.StartArray);
        }

        #endregion

        #region Private Methods

        private void AssertCanRead()
        {
            if( !this.reader.Read() )
                throw new EndOfStreamException();
        }

        private void AssertToken( JsonToken token )
        {
            if( this.reader.TokenType != token )
                throw new FormatException($"Expected token: {token}; actual token: {this.reader.TokenType}.");
        }

        private void AssertToken( JsonToken token1, JsonToken token2 )
        {
            if( this.reader.TokenType != token1
             && this.reader.TokenType != token2 )
                throw new FormatException($"Expected tokens: [{token1}, {token2}]; actual token: {this.reader.TokenType}.");
        }

        private void ReadToken( JsonToken token )
        {
            this.AssertCanRead();
            this.AssertToken(token);
        }

        private void ReadPropertyName( string propertyName )
        {
            this.ReadToken(JsonToken.PropertyName);
            if( !string.Equals((string)this.reader.Value, propertyName, StringComparison.Ordinal) )
                throw new FormatException($@"Expected property name: ""{propertyName}""; actual property name: ""{(string)this.reader.Value}"".");
        }

        #endregion

        #region Disposable

        /// <summary>
        /// Releases managed (IDisposable) resources.
        /// </summary>
        protected override void OnDispose()
        {
            this.reader.Close();
        }

        #endregion

        #region IEventStreamReader

        /// <summary>
        /// Gets a value indicating whether this instance expects to read size optimized content.
        /// </summary>
        public bool IsCompactFormat => false;

        /// <summary>
        /// Tries to read the next event from the stream.
        /// </summary>
        /// <returns>The <see cref="IEventReader"/> of the next event; or <c>null</c> if there are no more events</returns>
        public IEventReader TryRead()
        {
            this.ThrowIfDisposed();

            // we may be in the middle of an event array (IsAtEnd == false)
            // let's try to find the next event array
            while( this.reader.Read() )
            {
                if( this.reader.TokenType == JsonToken.StartArray )
                {
                    this.AssertCanRead(); // move to first array item
                    return this;
                }
            }

            return null;
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

                return this.reader.TokenType == JsonToken.EndArray;
            }
        }

        /// <summary>
        /// Deserializes the next component of the event, as a(n) <see cref="byte"/> value.
        /// Behaviour is undetermined, if this is not the same type of value that was originally serialized.
        /// </summary>
        /// <returns>The deserialized value.</returns>
        public byte ReadUInt8()
        {
            return (byte)this.ReadInt64();
        }

        /// <summary>
        /// Deserializes the next component of the event, as a(n) <see cref="int"/> value.
        /// Behaviour is undetermined, if this is not the same type of value that was originally serialized.
        /// </summary>
        /// <returns>The deserialized value.</returns>
        public int ReadInt32()
        {
            return (int)this.ReadInt64();
        }

        /// <summary>
        /// Deserializes the next component of the event, as a(n) <see cref="long"/> value.
        /// Behaviour is undetermined, if this is not the same type of value that was originally serialized.
        /// </summary>
        /// <returns>The deserialized value.</returns>
        public long ReadInt64()
        {
            this.ThrowIfDisposed();
            this.AssertToken(JsonToken.Integer);

            var result = (long)this.reader.Value;
            this.AssertCanRead();
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
            this.AssertToken(JsonToken.Float);

            var result = (float)((double)this.reader.Value);
            this.AssertCanRead();
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
            this.AssertToken(JsonToken.String, JsonToken.Null);

            var result = (string)this.reader.Value;
            this.AssertCanRead();
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
            this.AssertToken(JsonToken.Boolean);

            var result = (bool)this.reader.Value;
            this.AssertCanRead();
            return result;
        }

        #endregion
    }
}
