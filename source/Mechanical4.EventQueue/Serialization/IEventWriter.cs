namespace Mechanical4.EventQueue.Serialization
{
    /// <summary>
    /// Used to serialize components of a single event instance, while preserving their order.
    /// </summary>
    public interface IEventWriter
    {
        /// <summary>
        /// Serializes the specified value.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        void Write( byte value );

        /// <summary>
        /// Serializes the specified value.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        void Write( int value );

        /// <summary>
        /// Serializes the specified value.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        void Write( long value );

        /// <summary>
        /// Serializes the specified value.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        void Write( string value );

        /// <summary>
        /// Serializes the specified value.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        void Write( bool value );
    }
}
