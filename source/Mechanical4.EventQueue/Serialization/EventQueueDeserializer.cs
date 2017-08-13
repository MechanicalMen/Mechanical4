using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Mechanical4.Core;

namespace Mechanical4.EventQueue.Serialization
{
    /// <summary>
    /// Deserializes events using an <see cref="IEventStreamReader"/>.
    /// Thread-safe.
    /// </summary>
    public class EventQueueDeserializer : Disposable.NonBlockingBase
    {
        #region Private Fields

        private readonly Dictionary<int, Func<IEventReader, SerializableEventBase>> constructorsByIntID = new Dictionary<int, Func<IEventReader, SerializableEventBase>>();
        private readonly Dictionary<string, Func<IEventReader, SerializableEventBase>> constructorsByStringID = new Dictionary<string, Func<IEventReader, SerializableEventBase>>();
        private readonly Dictionary<int, string> sourcePositionByIntID = new Dictionary<int, string>();
        private readonly IEventStreamReader streamReader;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="IEventStreamReader"/> class.
        /// </summary>
        /// <param name="reader">The <see cref="IEventStreamReader"/> used to load events.</param>
        public EventQueueDeserializer( IEventStreamReader reader )
        {
            this.streamReader = reader ?? throw Exc.Null(nameof(reader));

            // read and verify format version
            var eventReader = this.streamReader.TryRead();
            if( eventReader.NullReference() )
                throw new FormatException("Invalid event stream: stream format version not found! (unexpected end of stream)");

            var streamFormatVersion = eventReader.ReadInt32();
            if( streamFormatVersion != EventQueueSerializer.FormatVersion )
                throw new FormatException($"The {nameof(EventQueueSerializer)} format can not be parsed! (expected version: {EventQueueSerializer.FormatVersion}; actual version: {streamFormatVersion})");
        }

        #endregion

        #region Private Static Methods

        private static Func<IEventReader, SerializableEventBase> CreateConstructor( Type eventType )
        {
            var eventTypeInfo = eventType.GetTypeInfo();
            if( eventTypeInfo.IsAssignableFrom(typeof(SerializableEventBase).GetTypeInfo()) )
                throw new ArgumentException($"Event type does not inherit from {nameof(SerializableEventBase)}!");

            // find the first applicable constructor
            var constructor = eventTypeInfo
                .DeclaredConstructors
                .Where(ctor =>
                {
                    if( !ctor.IsPublic )
                        return false;

                    var parameters = ctor.GetParameters();
                    if( (parameters?.Length ?? 0) != 1 )
                        return false;

                    return parameters[0].ParameterType == typeof(IEventReader);
                })
                .FirstOrDefault();
            if( constructor.NullReference() )
                throw new MissingMemberException($"Could not find constructor of type {eventType.FullName}!");

            var parameter = LambdaExpression.Parameter(typeof(IEventReader));
            var body = LambdaExpression.New(constructor, parameter);
            return LambdaExpression.Lambda<Func<IEventReader, SerializableEventBase>>(body, parameter).Compile();
        }

        private static int Read7BitEncoded( IEventReader reader )
        {
            uint actualValue = 0;
            int numBitsDecoded = 0;
            while( true )
            {
                var encodedByte = reader.ReadUInt8();
                bool hasMore = VariableLengthUInt.DecodeByte(encodedByte, ref actualValue, ref numBitsDecoded);
                if( !hasMore )
                    break;
            }
            return VariableLengthUInt.ToSigned(actualValue);
        }

        #endregion

        #region Disposable

        /// <summary>
        /// Releases managed (IDisposable) resources.
        /// </summary>
        protected override void OnDispose()
        {
            this.streamReader.Dispose();
        }

        #endregion

        #region Private Methods

        private SerializableEventBase ReadNextEvent()
        {
            var eventReader = this.streamReader.TryRead();
            if( eventReader.NullReference() )
                return null;

            // load event constructor
            Func<IEventReader, SerializableEventBase> constructor;
            if( !this.streamReader.IsCompactFormat )
            {
                string assemblyQualifiedName = eventReader.ReadString();
                if( !this.constructorsByStringID.TryGetValue(assemblyQualifiedName, out constructor) )
                {
                    var eventType = Type.GetType(assemblyQualifiedName);
                    constructor = CreateConstructor(eventType);
                    this.constructorsByStringID.Add(assemblyQualifiedName, constructor);
                }
            }
            else
            {
                int typeID = Read7BitEncoded(eventReader);
                if( !this.constructorsByIntID.TryGetValue(typeID, out constructor) )
                {
                    string assemblyQualifiedName = eventReader.ReadString();
                    var eventType = Type.GetType(assemblyQualifiedName);
                    constructor = CreateConstructor(eventType);
                    this.constructorsByIntID.Add(typeID, constructor);
                }
            }

            // read source position
            string sourcePos;
            if( !this.streamReader.IsCompactFormat )
            {
                sourcePos = eventReader.ReadString();
            }
            else
            {
                int posID = Read7BitEncoded(eventReader);
                if( posID == 0 )
                {
                    sourcePos = null;
                }
                else if( !this.sourcePositionByIntID.TryGetValue(posID, out sourcePos) )
                {
                    sourcePos = eventReader.ReadString();
                    this.sourcePositionByIntID.Add(posID, sourcePos);
                }
            }

            // read timestamp
            DateTime timestamp;
            if( !this.streamReader.IsCompactFormat )
                timestamp = DateTime.ParseExact(eventReader.ReadString(), "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            else
                timestamp = new DateTime(eventReader.ReadInt64(), DateTimeKind.Utc);

            // create event instance
            var evnt = constructor(eventReader);
            evnt.EventEnqueuePos = sourcePos;
            evnt.EventEnqueueTime = timestamp;
            return evnt;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Tries to deserialize the next event from the stream.
        /// Returns <c>null</c>, if there are no more events to read.
        /// </summary>
        /// <returns>The event deserialized; or <c>null</c>.</returns>
        public SerializableEventBase Read()
        {
            this.ThrowIfDisposed();

            return this.ReadNextEvent();
        }

        #endregion
    }
}
