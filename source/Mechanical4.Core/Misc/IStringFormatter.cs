namespace Mechanical4.Core.Misc
{
    /// <summary>
    /// Converts from type <typeparamref name="T"/> to <see cref="string"/>.
    /// Expected to be thread-safe.
    /// </summary>
    /// <typeparam name="T">The type to convert from.</typeparam>
    public interface IStringFormatter<in T>
    {
        //// NOTE: we do not output null strings, so that all possible outputs can be easily represented in a text file.

        /// <summary>
        /// Converts the specified object to a <see cref="string"/>.
        /// Does not return <c>null</c>.
        /// </summary>
        /// <param name="obj">The object to convert to a string.</param>
        /// <returns>The string representation of the specified object. Never <c>null</c>.</returns>
        string ToString( T obj );
    }
}
