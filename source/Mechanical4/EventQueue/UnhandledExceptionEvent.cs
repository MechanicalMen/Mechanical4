using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Mechanical4.EventQueue
{
    /// <summary>
    /// Converts an exception into a string.
    /// It is recommended that it's handlers be very quick,
    /// and launch a separate event, if long-running work
    /// (like collecting debugging data) is required.
    /// </summary>
    public class UnhandledExceptionEvent : EventBase
    {
        #region Private Fields

        private string fullString;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="UnhandledExceptionEvent"/> class.
        /// </summary>
        /// <param name="exception">The exception to report.</param>
        public UnhandledExceptionEvent( Exception exception )
        {
            if( exception.NullReference() )
                throw Exc.Null(nameof(exception));

            this.Initialize(
                type: exception.GetType().ToString(),
                message: exception.Message,
                full: ToString(exception));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnhandledExceptionEvent"/> class.
        /// </summary>
        /// <param name="type">The exception type.</param>
        /// <param name="message">The exception message.</param>
        /// <param name="asString">The serialized exception string.</param>
        public UnhandledExceptionEvent( string type, string message, string asString )
        {
            if( type.NullOrEmpty() )
                throw new ArgumentException("Invalid type!");

            if( message.NullOrEmpty() )
                throw new ArgumentException("Invalid message!");

            if( asString.NullOrEmpty() )
                throw new ArgumentException("Invalid exception string!");

            this.Initialize(
                type.Trim(),
                message.Trim(),
                asString.Trim());
        }

        private void Initialize( string type, string message, string full )
        {
            if( this.fullString.NotNullReference() )
                throw new InvalidOperationException("Already initialized!");

            this.Type = type;
            this.Message = message;
            this.fullString = full;
        }

        #endregion

        #region Private Static Methods

        private static string ToString( Exception exception )
        {
            var sb = new StringBuilder();
            Append(sb, exception, singleIndentationLevel: "  ");
            return sb.ToString();
        }

        private static void Append( StringBuilder sb, Exception exception, string singleIndentationLevel, string currentIndentation = "" )
        {
            sb.Append(currentIndentation);
            sb.Append("Type: ");
            sb.Append(exception.GetType().ToString());

            if( !exception.Message.NullOrWhiteSpace() )
            {
                sb.AppendLine();
                sb.Append(currentIndentation);
                sb.Append("Message: ");
                sb.Append(exception.Message);
            }

            var data = GetExceptionData(exception).ToArray();
            if( data.Length > 0 )
            {
                sb.AppendLine();
                sb.Append(currentIndentation);
                sb.Append("Data:"); // no newline here

                foreach( var pair in data )
                {
                    sb.AppendLine(); // newline here
                    sb.Append(currentIndentation);
                    sb.Append(singleIndentationLevel);
                    sb.Append(pair.Key);
                    sb.Append(" = ");
                    sb.Append(pair.Value);
                    //// no newline here
                }
            }

            var stackTrace = exception.StackTrace;
            if( !stackTrace.NullOrEmpty() ) // this can actually happen
            {
                //// NOTE: we print this the end, because it is easy to miss info below large stack traces
                sb.AppendLine();
                sb.Append(currentIndentation);
                sb.AppendLine("StackTrace:");
                AppendReindented(sb, stackTrace, currentIndentation + singleIndentationLevel);
            }

            // repeat for inner exceptions
            var innerExceptions = GetInnerExceptions(exception);
            if( (innerExceptions?.Length ?? 0) != 0 )
            {
                if( innerExceptions.Length == 1 )
                {
                    sb.AppendLine();
                    sb.AppendLine();
                    sb.AppendLine();

                    sb.Append(currentIndentation);
                    sb.AppendLine("InnerException:");
                    Append(sb, innerExceptions[0], singleIndentationLevel, currentIndentation + singleIndentationLevel);
                }
                else
                {
                    for( int i = 0; i < innerExceptions.Length; ++i )
                    {
                        sb.AppendLine();
                        sb.AppendLine();
                        sb.AppendLine();

                        sb.Append(currentIndentation);
                        sb.Append("InnerExceptions[");
                        sb.Append(i.ToString("D", CultureInfo.InvariantCulture));
                        sb.AppendLine("]:");
                        Append(sb, innerExceptions[i], singleIndentationLevel, currentIndentation + singleIndentationLevel);
                    }
                }
            }
        }

        private static IEnumerable<KeyValuePair<string, string>> GetExceptionData( Exception exception )
        {
            string key;
            foreach( DictionaryEntry entry in exception.Data )
            {
                key = entry.Key?.ToString();
                if( key.NotNullReference() )
                    yield return new KeyValuePair<string, string>(key, entry.Value?.ToString());
            }
        }

        private static void AppendReindented( StringBuilder sb, string str, string newIndentation )
        {
            using( var reader = new StringReader(str) )
            {
                bool firstLine = true;
                string line;
                while( (line = reader.ReadLine()).NotNullReference() )
                {
                    if( firstLine )
                        firstLine = false;
                    else
                        sb.AppendLine();

                    sb.Append(newIndentation);
                    sb.Append(line.Trim());
                }
            }
        }

        private static Exception[] GetInnerExceptions( Exception exception )
        {
            if( exception.InnerException.NullReference() )
                return null;
            else
                return exception is AggregateException ae ? ae.InnerExceptions.ToArray() : new Exception[] { exception.InnerException };
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Gets the exception type.
        /// </summary>
        public string Type { get; private set; }

        /// <summary>
        /// Gets the exception message.
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// Gets the serialized exception string.
        /// </summary>
        /// <returns>The exception string.</returns>
        public override string ToString()
        {
            return this.fullString;
        }

        #endregion
    }
}
