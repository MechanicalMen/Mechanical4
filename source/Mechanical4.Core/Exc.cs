using System;

namespace Mechanical4.Core
{
    /// <summary>
    /// Helps throw common exceptions.
    /// </summary>
    public static class Exc
    {
        /// <summary>
        /// Returns an exception caused by a <c>null</c> parameter.
        /// </summary>
        /// <param name="paramName">The name of the parameter that caused the exception.</param>
        /// <returns>A new <see cref="ArgumentNullException"/> instance.</returns>
        public static ArgumentNullException Null( string paramName )
        {
            return new ArgumentNullException(paramName);
        }
    }
}
