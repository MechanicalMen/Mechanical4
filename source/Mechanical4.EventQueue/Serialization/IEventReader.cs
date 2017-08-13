namespace Mechanical4.EventQueue.Serialization
{
    /// <summary>
    /// Used to deserialize the components of a single event instance, in the order they were serialized.
    /// </summary>
    public interface IEventReader
    {
        /// <summary>
        /// Gets a value indicating whether there are no more components of the event left to read.
        /// </summary>
        bool IsAtEnd { get; }

        /// <summary>
        /// Deserializes the next component of the event, as a(n) <see cref="byte"/> value.
        /// Behaviour is undetermined, if this is not the same type of value that was originally serialized.
        /// </summary>
        /// <returns>The deserialized value.</returns>
        byte ReadUInt8();

        /// <summary>
        /// Deserializes the next component of the event, as a(n) <see cref="int"/> value.
        /// Behaviour is undetermined, if this is not the same type of value that was originally serialized.
        /// </summary>
        /// <returns>The deserialized value.</returns>
        int ReadInt32();

        /// <summary>
        /// Deserializes the next component of the event, as a(n) <see cref="long"/> value.
        /// Behaviour is undetermined, if this is not the same type of value that was originally serialized.
        /// </summary>
        /// <returns>The deserialized value.</returns>
        long ReadInt64();

        /// <summary>
        /// Deserializes the next component of the event, as a(n) <see cref="float"/> value.
        /// Behaviour is undetermined, if this is not the same type of value that was originally serialized.
        /// </summary>
        /// <returns>The deserialized value.</returns>
        float ReadSingle();

        /// <summary>
        /// Deserializes the next component of the event, as a(n) <see cref="string"/> value.
        /// Behaviour is undetermined, if this is not the same type of value that was originally serialized.
        /// </summary>
        /// <returns>The deserialized value.</returns>
        string ReadString();

        /// <summary>
        /// Deserializes the next component of the event, as a(n) <see cref="bool"/> value.
        /// Behaviour is undetermined, if this is not the same type of value that was originally serialized.
        /// </summary>
        /// <returns>The deserialized value.</returns>
        bool ReadBoolean();
    }
}
