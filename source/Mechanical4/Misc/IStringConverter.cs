namespace Mechanical4.Misc
{
    /// <summary>
    /// Converts between type <typeparamref name="T"/> and <see cref="string"/>.
    /// Expected to be thread-safe.
    /// </summary>
    /// <typeparam name="T">The type to convert.</typeparam>
    public interface IStringConverter<T> : IStringFormatter<T>
    {
        //// NOTE: we do not accept null strings, to be consistent with IStringFormatter

        /// <summary>
        /// Tries to parse the specified <see cref="string"/>.
        /// </summary>
        /// <param name="str">The string representation to parse. Not <c>null</c>.</param>
        /// <param name="obj">The result of a successful parsing; or otherwise the default instance of the type.</param>
        /// <returns><c>true</c> if the string was successfully parsed; otherwise, <c>false</c>.</returns>
        bool TryParse( string str, out T obj );
    }
}
