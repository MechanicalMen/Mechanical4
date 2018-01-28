using System;
using System.Runtime.CompilerServices;

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
        /// <param name="file">The source file that contains the caller.</param>
        /// <param name="member">The method or property name of the caller to this method.</param>
        /// <param name="line">The line number in the source file at which this method is called.</param>
        /// <returns>A new <see cref="ArgumentNullException"/> instance.</returns>
        public static ArgumentNullException Null(
            string paramName,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
        {
            return new ArgumentNullException(paramName).StoreFileLine(file, member, line);
        }
    }
}
