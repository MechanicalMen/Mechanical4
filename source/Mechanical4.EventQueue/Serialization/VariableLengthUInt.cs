using System;

namespace Mechanical4.EventQueue.Serialization
{
    /// <summary>
    /// Helps handling 7-bit encoded unsigned integers.
    /// </summary>
    public static class VariableLengthUInt
    {
        //// NOTE: through we only encode unsigned integers here, signed integers can be converted to unsigned integers

        /// <summary>
        /// The maximum number of bytes necessary to represent any <see cref="uint"/> value.
        /// </summary>
        public const int MaximumNumberOfBytes = 5;

        //// NOTE: 127 is the maximum value representable on 7 bits
        private const byte MoreBytesFlag = 0b1000_0000; // 128
        private const byte BitMask = 0b0111_1111; // 127

        /// <summary>
        /// Encodes the next byte of the specified unsigned integer.
        /// </summary>
        /// <param name="value">The remaining unencoded integer to encode.</param>
        /// <param name="nextEncodedByte">The next encoded byte.</param>
        /// <returns><c>true</c> if at least one more byte needs to be encoded; otherwise, <c>false</c>.</returns>
        public static bool EncodeNextByte( ref uint value, out byte nextEncodedByte )
        {
            if( value > 127 )
            {
                nextEncodedByte = (byte)((value & BitMask) | MoreBytesFlag);
                value >>= 7;
                return true;
            }
            else
            {
                nextEncodedByte = (byte)value;
                value = 0;
                return false;
            }
        }

        /// <summary>
        /// Decodes a single byte of an encoded integer.
        /// </summary>
        /// <param name="encodedByte">The byte to decode.</param>
        /// <param name="value">The decoded part of the integer.</param>
        /// <param name="numBitsDecoded">The number of bits decoded so far.</param>
        /// <returns><c>true</c> if there is at least one more byte that needs to be decoded; otherwise, <c>false</c>.</returns>
        public static bool DecodeByte( byte encodedByte, ref uint value, ref int numBitsDecoded )
        {
            // append bits
            value |= (uint)((encodedByte & BitMask) << numBitsDecoded);
            numBitsDecoded += 7;

            // expect more bytes?
            bool moreToDecode = (encodedByte & MoreBytesFlag) != 0;
            if( moreToDecode )
            {
                if( numBitsDecoded == MaximumNumberOfBytes * 7 )
                    throw new FormatException("7-bit encoding error: encoded bits indicate at least one encoded byte is available, but none were expected! The data source was corrupted, or it does not store a 7-bit encoded integer at the specified position.");

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Maps a signed integer to an unsigned integer.
        /// </summary>
        /// <param name="value">The signed integer to convert.</param>
        /// <returns>The converted integer.</returns>
        public static uint ToUnsigned( int value )
        {
            unchecked
            {
                return (uint)value;
            }
        }

        /// <summary>
        /// Maps an unsigned integer to a signed integer.
        /// </summary>
        /// <param name="value">The unsigned integer to convert.</param>
        /// <returns>The converted integer.</returns>
        public static int ToSigned( uint value )
        {
            unchecked
            {
                return (int)value;
            }
        }
    }
}
