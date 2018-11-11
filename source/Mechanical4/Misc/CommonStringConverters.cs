using System;
using System.Globalization;

namespace Mechanical4.Misc
{
    /// <summary>
    /// <see cref="IStringConverter{T}"/> implementations for the most commonly used types.
    /// This mainly consist of the built-in base types.
    /// </summary>
    public static class CommonStringConverters
    {
        #region SByte

        /// <summary>
        /// Implements an <see cref="IStringConverter{T}"/> for <see cref="sbyte"/>.
        /// </summary>
        public class SByte : IStringConverter<sbyte>
        {
            private readonly IFormatProvider provider;
            private readonly string format;
            private readonly NumberStyles style;

            /// <summary>
            /// Initializes a new instance of the <see cref="SByte"/> class.
            /// </summary>
            /// <param name="formatProvider">An object that provides culture-specific formatting information.</param>
            /// <param name="formatString">A standard or custom numeric format string.</param>
            /// <param name="numberStyles">The style elements that can be present in strings to be parsed.</param>
            public SByte( IFormatProvider formatProvider, string formatString, NumberStyles numberStyles )
            {
                this.provider = formatProvider;
                this.format = formatString;
                this.style = numberStyles;
            }

            /// <summary>
            /// Converts the specified object to a <see cref="string"/>.
            /// Does not return <c>null</c>.
            /// </summary>
            /// <param name="obj">The object to convert to a string.</param>
            /// <returns>The string representation of the specified object. Never <c>null</c>.</returns>
            public string ToString( sbyte obj )
            {
                return obj.ToString(this.format, this.provider);
            }

            /// <summary>
            /// Tries to parse the specified <see cref="string"/>.
            /// </summary>
            /// <param name="str">The string representation to parse. Not <c>null</c>.</param>
            /// <param name="obj">The result of a successful parsing; or otherwise the default instance of the type.</param>
            /// <returns><c>true</c> if the string was successfully parsed; otherwise, <c>false</c>.</returns>
            public bool TryParse( string str, out sbyte obj )
            {
                if( str.NullReference() )
                {
                    obj = default(sbyte);
                    return false;
                }

                return sbyte.TryParse(str, this.style, this.provider, out obj);
            }
        }

        #endregion

        #region Byte

        /// <summary>
        /// Implements an <see cref="IStringConverter{T}"/> for <see cref="byte"/>.
        /// </summary>
        public class Byte : IStringConverter<byte>
        {
            private readonly IFormatProvider provider;
            private readonly string format;
            private readonly NumberStyles style;

            /// <summary>
            /// Initializes a new instance of the <see cref="Byte"/> class.
            /// </summary>
            /// <param name="formatProvider">An object that provides culture-specific formatting information.</param>
            /// <param name="formatString">A standard or custom numeric format string.</param>
            /// <param name="numberStyles">The style elements that can be present in strings to be parsed.</param>
            public Byte( IFormatProvider formatProvider, string formatString, NumberStyles numberStyles )
            {
                this.provider = formatProvider;
                this.format = formatString;
                this.style = numberStyles;
            }

            /// <summary>
            /// Converts the specified object to a <see cref="string"/>.
            /// Does not return <c>null</c>.
            /// </summary>
            /// <param name="obj">The object to convert to a string.</param>
            /// <returns>The string representation of the specified object. Never <c>null</c>.</returns>
            public string ToString( byte obj )
            {
                return obj.ToString(this.format, this.provider);
            }

            /// <summary>
            /// Tries to parse the specified <see cref="string"/>.
            /// </summary>
            /// <param name="str">The string representation to parse. Not <c>null</c>.</param>
            /// <param name="obj">The result of a successful parsing; or otherwise the default instance of the type.</param>
            /// <returns><c>true</c> if the string was successfully parsed; otherwise, <c>false</c>.</returns>
            public bool TryParse( string str, out byte obj )
            {
                if( str.NullReference() )
                {
                    obj = default(byte);
                    return false;
                }

                return byte.TryParse(str, this.style, this.provider, out obj);
            }
        }

        #endregion

        #region Int16

        /// <summary>
        /// Implements an <see cref="IStringConverter{T}"/> for <see cref="short"/>.
        /// </summary>
        public class Int16 : IStringConverter<short>
        {
            private readonly IFormatProvider provider;
            private readonly string format;
            private readonly NumberStyles style;

            /// <summary>
            /// Initializes a new instance of the <see cref="Int16"/> class.
            /// </summary>
            /// <param name="formatProvider">An object that provides culture-specific formatting information.</param>
            /// <param name="formatString">A standard or custom numeric format string.</param>
            /// <param name="numberStyles">The style elements that can be present in strings to be parsed.</param>
            public Int16( IFormatProvider formatProvider, string formatString, NumberStyles numberStyles )
            {
                this.provider = formatProvider;
                this.format = formatString;
                this.style = numberStyles;
            }

            /// <summary>
            /// Converts the specified object to a <see cref="string"/>.
            /// Does not return <c>null</c>.
            /// </summary>
            /// <param name="obj">The object to convert to a string.</param>
            /// <returns>The string representation of the specified object. Never <c>null</c>.</returns>
            public string ToString( short obj )
            {
                return obj.ToString(this.format, this.provider);
            }

            /// <summary>
            /// Tries to parse the specified <see cref="string"/>.
            /// </summary>
            /// <param name="str">The string representation to parse. Not <c>null</c>.</param>
            /// <param name="obj">The result of a successful parsing; or otherwise the default instance of the type.</param>
            /// <returns><c>true</c> if the string was successfully parsed; otherwise, <c>false</c>.</returns>
            public bool TryParse( string str, out short obj )
            {
                if( str.NullReference() )
                {
                    obj = default(short);
                    return false;
                }

                return short.TryParse(str, this.style, this.provider, out obj);
            }
        }

        #endregion

        #region UInt16

        /// <summary>
        /// Implements an <see cref="IStringConverter{T}"/> for <see cref="ushort"/>.
        /// </summary>
        public class UInt16 : IStringConverter<ushort>
        {
            private readonly IFormatProvider provider;
            private readonly string format;
            private readonly NumberStyles style;

            /// <summary>
            /// Initializes a new instance of the <see cref="UInt16"/> class.
            /// </summary>
            /// <param name="formatProvider">An object that provides culture-specific formatting information.</param>
            /// <param name="formatString">A standard or custom numeric format string.</param>
            /// <param name="numberStyles">The style elements that can be present in strings to be parsed.</param>
            public UInt16( IFormatProvider formatProvider, string formatString, NumberStyles numberStyles )
            {
                this.provider = formatProvider;
                this.format = formatString;
                this.style = numberStyles;
            }

            /// <summary>
            /// Converts the specified object to a <see cref="string"/>.
            /// Does not return <c>null</c>.
            /// </summary>
            /// <param name="obj">The object to convert to a string.</param>
            /// <returns>The string representation of the specified object. Never <c>null</c>.</returns>
            public string ToString( ushort obj )
            {
                return obj.ToString(this.format, this.provider);
            }

            /// <summary>
            /// Tries to parse the specified <see cref="string"/>.
            /// </summary>
            /// <param name="str">The string representation to parse. Not <c>null</c>.</param>
            /// <param name="obj">The result of a successful parsing; or otherwise the default instance of the type.</param>
            /// <returns><c>true</c> if the string was successfully parsed; otherwise, <c>false</c>.</returns>
            public bool TryParse( string str, out ushort obj )
            {
                if( str.NullReference() )
                {
                    obj = default(ushort);
                    return false;
                }

                return ushort.TryParse(str, this.style, this.provider, out obj);
            }
        }

        #endregion

        #region Int32

        /// <summary>
        /// Implements an <see cref="IStringConverter{T}"/> for <see cref="int"/>.
        /// </summary>
        public class Int32 : IStringConverter<int>
        {
            private readonly IFormatProvider provider;
            private readonly string format;
            private readonly NumberStyles style;

            /// <summary>
            /// Initializes a new instance of the <see cref="Int32"/> class.
            /// </summary>
            /// <param name="formatProvider">An object that provides culture-specific formatting information.</param>
            /// <param name="formatString">A standard or custom numeric format string.</param>
            /// <param name="numberStyles">The style elements that can be present in strings to be parsed.</param>
            public Int32( IFormatProvider formatProvider, string formatString, NumberStyles numberStyles )
            {
                this.provider = formatProvider;
                this.format = formatString;
                this.style = numberStyles;
            }

            /// <summary>
            /// Converts the specified object to a <see cref="string"/>.
            /// Does not return <c>null</c>.
            /// </summary>
            /// <param name="obj">The object to convert to a string.</param>
            /// <returns>The string representation of the specified object. Never <c>null</c>.</returns>
            public string ToString( int obj )
            {
                return obj.ToString(this.format, this.provider);
            }

            /// <summary>
            /// Tries to parse the specified <see cref="string"/>.
            /// </summary>
            /// <param name="str">The string representation to parse. Not <c>null</c>.</param>
            /// <param name="obj">The result of a successful parsing; or otherwise the default instance of the type.</param>
            /// <returns><c>true</c> if the string was successfully parsed; otherwise, <c>false</c>.</returns>
            public bool TryParse( string str, out int obj )
            {
                if( str.NullReference() )
                {
                    obj = default(int);
                    return false;
                }

                return int.TryParse(str, this.style, this.provider, out obj);
            }
        }

        #endregion

        #region UInt32

        /// <summary>
        /// Implements an <see cref="IStringConverter{T}"/> for <see cref="uint"/>.
        /// </summary>
        public class UInt32 : IStringConverter<uint>
        {
            private readonly IFormatProvider provider;
            private readonly string format;
            private readonly NumberStyles style;

            /// <summary>
            /// Initializes a new instance of the <see cref="UInt32"/> class.
            /// </summary>
            /// <param name="formatProvider">An object that provides culture-specific formatting information.</param>
            /// <param name="formatString">A standard or custom numeric format string.</param>
            /// <param name="numberStyles">The style elements that can be present in strings to be parsed.</param>
            public UInt32( IFormatProvider formatProvider, string formatString, NumberStyles numberStyles )
            {
                this.provider = formatProvider;
                this.format = formatString;
                this.style = numberStyles;
            }

            /// <summary>
            /// Converts the specified object to a <see cref="string"/>.
            /// Does not return <c>null</c>.
            /// </summary>
            /// <param name="obj">The object to convert to a string.</param>
            /// <returns>The string representation of the specified object. Never <c>null</c>.</returns>
            public string ToString( uint obj )
            {
                return obj.ToString(this.format, this.provider);
            }

            /// <summary>
            /// Tries to parse the specified <see cref="string"/>.
            /// </summary>
            /// <param name="str">The string representation to parse. Not <c>null</c>.</param>
            /// <param name="obj">The result of a successful parsing; or otherwise the default instance of the type.</param>
            /// <returns><c>true</c> if the string was successfully parsed; otherwise, <c>false</c>.</returns>
            public bool TryParse( string str, out uint obj )
            {
                if( str.NullReference() )
                {
                    obj = default(uint);
                    return false;
                }

                return uint.TryParse(str, this.style, this.provider, out obj);
            }
        }

        #endregion

        #region Int64

        /// <summary>
        /// Implements an <see cref="IStringConverter{T}"/> for <see cref="long"/>.
        /// </summary>
        public class Int64 : IStringConverter<long>
        {
            private readonly IFormatProvider provider;
            private readonly string format;
            private readonly NumberStyles style;

            /// <summary>
            /// Initializes a new instance of the <see cref="Int64"/> class.
            /// </summary>
            /// <param name="formatProvider">An object that provides culture-specific formatting information.</param>
            /// <param name="formatString">A standard or custom numeric format string.</param>
            /// <param name="numberStyles">The style elements that can be present in strings to be parsed.</param>
            public Int64( IFormatProvider formatProvider, string formatString, NumberStyles numberStyles )
            {
                this.provider = formatProvider;
                this.format = formatString;
                this.style = numberStyles;
            }

            /// <summary>
            /// Converts the specified object to a <see cref="string"/>.
            /// Does not return <c>null</c>.
            /// </summary>
            /// <param name="obj">The object to convert to a string.</param>
            /// <returns>The string representation of the specified object. Never <c>null</c>.</returns>
            public string ToString( long obj )
            {
                return obj.ToString(this.format, this.provider);
            }

            /// <summary>
            /// Tries to parse the specified <see cref="string"/>.
            /// </summary>
            /// <param name="str">The string representation to parse. Not <c>null</c>.</param>
            /// <param name="obj">The result of a successful parsing; or otherwise the default instance of the type.</param>
            /// <returns><c>true</c> if the string was successfully parsed; otherwise, <c>false</c>.</returns>
            public bool TryParse( string str, out long obj )
            {
                if( str.NullReference() )
                {
                    obj = default(long);
                    return false;
                }

                return long.TryParse(str, this.style, this.provider, out obj);
            }
        }

        #endregion

        #region UInt64

        /// <summary>
        /// Implements an <see cref="IStringConverter{T}"/> for <see cref="ulong"/>.
        /// </summary>
        public class UInt64 : IStringConverter<ulong>
        {
            private readonly IFormatProvider provider;
            private readonly string format;
            private readonly NumberStyles style;

            /// <summary>
            /// Initializes a new instance of the <see cref="UInt64"/> class.
            /// </summary>
            /// <param name="formatProvider">An object that provides culture-specific formatting information.</param>
            /// <param name="formatString">A standard or custom numeric format string.</param>
            /// <param name="numberStyles">The style elements that can be present in strings to be parsed.</param>
            public UInt64( IFormatProvider formatProvider, string formatString, NumberStyles numberStyles )
            {
                this.provider = formatProvider;
                this.format = formatString;
                this.style = numberStyles;
            }

            /// <summary>
            /// Converts the specified object to a <see cref="string"/>.
            /// Does not return <c>null</c>.
            /// </summary>
            /// <param name="obj">The object to convert to a string.</param>
            /// <returns>The string representation of the specified object. Never <c>null</c>.</returns>
            public string ToString( ulong obj )
            {
                return obj.ToString(this.format, this.provider);
            }

            /// <summary>
            /// Tries to parse the specified <see cref="string"/>.
            /// </summary>
            /// <param name="str">The string representation to parse. Not <c>null</c>.</param>
            /// <param name="obj">The result of a successful parsing; or otherwise the default instance of the type.</param>
            /// <returns><c>true</c> if the string was successfully parsed; otherwise, <c>false</c>.</returns>
            public bool TryParse( string str, out ulong obj )
            {
                if( str.NullReference() )
                {
                    obj = default(ulong);
                    return false;
                }

                return ulong.TryParse(str, this.style, this.provider, out obj);
            }
        }

        #endregion

        #region Single

        /// <summary>
        /// Implements an <see cref="IStringConverter{T}"/> for <see cref="float"/>.
        /// </summary>
        public class Single : IStringConverter<float>
        {
            private readonly IFormatProvider provider;
            private readonly string format;
            private readonly NumberStyles style;

            /// <summary>
            /// Initializes a new instance of the <see cref="Single"/> class.
            /// </summary>
            /// <param name="formatProvider">An object that provides culture-specific formatting information.</param>
            /// <param name="formatString">A standard or custom numeric format string.</param>
            /// <param name="numberStyles">The style elements that can be present in strings to be parsed.</param>
            public Single( IFormatProvider formatProvider, string formatString, NumberStyles numberStyles )
            {
                this.provider = formatProvider;
                this.format = formatString;
                this.style = numberStyles;
            }

            /// <summary>
            /// Converts the specified object to a <see cref="string"/>.
            /// Does not return <c>null</c>.
            /// </summary>
            /// <param name="obj">The object to convert to a string.</param>
            /// <returns>The string representation of the specified object. Never <c>null</c>.</returns>
            public string ToString( float obj )
            {
                return obj.ToString(this.format, this.provider);
            }

            /// <summary>
            /// Tries to parse the specified <see cref="string"/>.
            /// </summary>
            /// <param name="str">The string representation to parse. Not <c>null</c>.</param>
            /// <param name="obj">The result of a successful parsing; or otherwise the default instance of the type.</param>
            /// <returns><c>true</c> if the string was successfully parsed; otherwise, <c>false</c>.</returns>
            public bool TryParse( string str, out float obj )
            {
                if( str.NullReference() )
                {
                    obj = default(float);
                    return false;
                }

                return float.TryParse(str, this.style, this.provider, out obj);
            }
        }

        #endregion

        #region Double

        /// <summary>
        /// Implements an <see cref="IStringConverter{T}"/> for <see cref="double"/>.
        /// </summary>
        public class Double : IStringConverter<double>
        {
            private readonly IFormatProvider provider;
            private readonly string format;
            private readonly NumberStyles style;

            /// <summary>
            /// Initializes a new instance of the <see cref="Double"/> class.
            /// </summary>
            /// <param name="formatProvider">An object that provides culture-specific formatting information.</param>
            /// <param name="formatString">A standard or custom numeric format string.</param>
            /// <param name="numberStyles">The style elements that can be present in strings to be parsed.</param>
            public Double( IFormatProvider formatProvider, string formatString, NumberStyles numberStyles )
            {
                this.provider = formatProvider;
                this.format = formatString;
                this.style = numberStyles;
            }

            /// <summary>
            /// Converts the specified object to a <see cref="string"/>.
            /// Does not return <c>null</c>.
            /// </summary>
            /// <param name="obj">The object to convert to a string.</param>
            /// <returns>The string representation of the specified object. Never <c>null</c>.</returns>
            public string ToString( double obj )
            {
                return obj.ToString(this.format, this.provider);
            }

            /// <summary>
            /// Tries to parse the specified <see cref="string"/>.
            /// </summary>
            /// <param name="str">The string representation to parse. Not <c>null</c>.</param>
            /// <param name="obj">The result of a successful parsing; or otherwise the default instance of the type.</param>
            /// <returns><c>true</c> if the string was successfully parsed; otherwise, <c>false</c>.</returns>
            public bool TryParse( string str, out double obj )
            {
                if( str.NullReference() )
                {
                    obj = default(double);
                    return false;
                }

                return double.TryParse(str, this.style, this.provider, out obj);
            }
        }

        #endregion

        #region Decimal

        /// <summary>
        /// Implements an <see cref="IStringConverter{T}"/> for <see cref="decimal"/>.
        /// </summary>
        public class Decimal : IStringConverter<decimal>
        {
            private readonly IFormatProvider provider;
            private readonly string format;
            private readonly NumberStyles style;

            /// <summary>
            /// Initializes a new instance of the <see cref="Decimal"/> class.
            /// </summary>
            /// <param name="formatProvider">An object that provides culture-specific formatting information.</param>
            /// <param name="formatString">A standard or custom numeric format string.</param>
            /// <param name="numberStyles">The style elements that can be present in strings to be parsed.</param>
            public Decimal( IFormatProvider formatProvider, string formatString, NumberStyles numberStyles )
            {
                this.provider = formatProvider;
                this.format = formatString;
                this.style = numberStyles;
            }

            /// <summary>
            /// Converts the specified object to a <see cref="string"/>.
            /// Does not return <c>null</c>.
            /// </summary>
            /// <param name="obj">The object to convert to a string.</param>
            /// <returns>The string representation of the specified object. Never <c>null</c>.</returns>
            public string ToString( decimal obj )
            {
                return obj.ToString(this.format, this.provider);
            }

            /// <summary>
            /// Tries to parse the specified <see cref="string"/>.
            /// </summary>
            /// <param name="str">The string representation to parse. Not <c>null</c>.</param>
            /// <param name="obj">The result of a successful parsing; or otherwise the default instance of the type.</param>
            /// <returns><c>true</c> if the string was successfully parsed; otherwise, <c>false</c>.</returns>
            public bool TryParse( string str, out decimal obj )
            {
                if( str.NullReference() )
                {
                    obj = default(decimal);
                    return false;
                }

                return decimal.TryParse(str, this.style, this.provider, out obj);
            }
        }

        #endregion

        #region Boolean

        /// <summary>
        /// Implements an <see cref="IStringConverter{T}"/> for <see cref="bool"/>.
        /// </summary>
        public class Boolean : IStringConverter<bool>
        {
            /// <summary>
            /// Converts the specified object to a <see cref="string"/>.
            /// Does not return <c>null</c>.
            /// </summary>
            /// <param name="obj">The object to convert to a string.</param>
            /// <returns>The string representation of the specified object. Never <c>null</c>.</returns>
            public string ToString( bool obj )
            {
                return obj ? "true" : "false";
            }

            /// <summary>
            /// Tries to parse the specified <see cref="string"/>.
            /// </summary>
            /// <param name="str">The string representation to parse. Not <c>null</c>.</param>
            /// <param name="obj">The result of a successful parsing; or otherwise the default instance of the type.</param>
            /// <returns><c>true</c> if the string was successfully parsed; otherwise, <c>false</c>.</returns>
            public bool TryParse( string str, out bool obj )
            {
                if( string.Equals(str, "true", StringComparison.OrdinalIgnoreCase) )
                {
                    obj = true;
                    return true;
                }
                else if( string.Equals(str, "false", StringComparison.OrdinalIgnoreCase) )
                {
                    obj = false;
                    return true;
                }

                obj = default(bool);
                return false;
            }
        }

        #endregion

        #region Char

        /// <summary>
        /// Implements an <see cref="IStringConverter{T}"/> for <see cref="char"/>.
        /// </summary>
        public class Char : IStringConverter<char>
        {
            /// <summary>
            /// Converts the specified object to a <see cref="string"/>.
            /// Does not return <c>null</c>.
            /// </summary>
            /// <param name="obj">The object to convert to a string.</param>
            /// <returns>The string representation of the specified object. Never <c>null</c>.</returns>
            public string ToString( char obj )
            {
                return obj.ToString();
            }

            /// <summary>
            /// Tries to parse the specified <see cref="string"/>.
            /// </summary>
            /// <param name="str">The string representation to parse. Not <c>null</c>.</param>
            /// <param name="obj">The result of a successful parsing; or otherwise the default instance of the type.</param>
            /// <returns><c>true</c> if the string was successfully parsed; otherwise, <c>false</c>.</returns>
            public bool TryParse( string str, out char obj )
            {
                if( str.NullReference()
                 || str.Length != 1 )
                {
                    obj = default(char);
                    return false;
                }
                else
                {
                    obj = str[0];
                    return true;
                }
            }
        }

        #endregion

        #region String

        /// <summary>
        /// Implements an <see cref="IStringConverter{T}"/> for <see cref="string"/>.
        /// </summary>
        public class String : IStringConverter<string>
        {
            /// <summary>
            /// Converts the specified object to a <see cref="string"/>.
            /// Does not return <c>null</c>.
            /// </summary>
            /// <param name="obj">The object to convert to a string.</param>
            /// <returns>The string representation of the specified object. Never <c>null</c>.</returns>
            public string ToString( string obj )
            {
                if( obj.NullReference() )
                    throw Exc.Null(nameof(obj));

                return obj;
            }

            /// <summary>
            /// Tries to parse the specified <see cref="string"/>.
            /// </summary>
            /// <param name="str">The string representation to parse. Not <c>null</c>.</param>
            /// <param name="obj">The result of a successful parsing; or otherwise the default instance of the type.</param>
            /// <returns><c>true</c> if the string was successfully parsed; otherwise, <c>false</c>.</returns>
            public bool TryParse( string str, out string obj )
            {
                if( str.NullReference() )
                {
                    obj = default(string);
                    return false;
                }
                else
                {
                    obj = str;
                    return true;
                }
            }
        }

        #endregion
    }
}
