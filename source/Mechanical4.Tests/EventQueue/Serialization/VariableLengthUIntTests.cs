using System.Collections.Generic;
using Mechanical4.EventQueue.Serialization;
using NUnit.Framework;

namespace Mechanical4.Tests.EventQueue.Serialization
{
    [TestFixture]
    public static class VariableLengthUIntTests
    {
        [Test]
        public static void SingleByte()
        {
            void TestEncoding( uint valueToEncode, bool expectedHasMore, uint expectedRemaining, byte expectedEncodedByte )
            {
                uint actualRemaining = valueToEncode;
                bool actualHasMore = VariableLengthUInt.EncodeNextByte(ref actualRemaining, out byte actualEncodedByte);
                Assert.AreEqual(expectedHasMore, actualHasMore);
                Assert.AreEqual(expectedRemaining, actualRemaining);
                Assert.AreEqual(actualEncodedByte, expectedEncodedByte);
            }
            void TestDecoding( byte byteToDecode, bool expectedHasMore, uint expectedValue )
            {
                uint actualValue = 0;
                int numBitsDecoded = 0;
                bool actualHasMore = VariableLengthUInt.DecodeByte(byteToDecode, ref actualValue, ref numBitsDecoded);
                Assert.AreEqual(expectedHasMore, actualHasMore);
                Assert.AreEqual(expectedValue, actualValue);
                Assert.AreEqual(7, numBitsDecoded);
            }
            void Test( uint value, bool expectedHasMore, uint expectedRemaining, byte expectedEncodedByte )
            {
                TestEncoding(value, expectedHasMore, expectedRemaining, expectedEncodedByte);
                TestDecoding(expectedEncodedByte, expectedHasMore, value & 127);
            }

            Test(0, false, 0, 0);
            Test(1, false, 0, 1);
            Test(127, false, 0, 127);
            Test(128, true, 1, 128);
        }

        [Test]
        public static void MultiByte()
        {
            void TestEncoding( uint valueToEncode, byte[] expectedBytes )
            {
                var actualBytes = new List<byte>();
                while( true )
                {
                    bool hasMore = VariableLengthUInt.EncodeNextByte(ref valueToEncode, out var encodedByte);
                    actualBytes.Add(encodedByte);
                    if( !hasMore )
                        break;
                }

                Assert.AreEqual(expectedBytes.Length, actualBytes.Count);
                for( int i = 0; i < expectedBytes.Length; ++i )
                {
                    if( expectedBytes[i] != actualBytes[i] )
                        Assert.Fail($"Byte mismatch at index {i}! Expected {expectedBytes[i]}, but was {actualBytes[i]}.");
                }
            }
            void TestDecoding( byte[] bytesToDecode, uint expectedValue )
            {
                uint actualValue = 0;
                int numBitsDecoded = 0;
                var bytes = bytesToDecode.GetEnumerator();
                while( true )
                {
                    if( !bytes.MoveNext() )
                        Assert.Fail($"More bytes expected!");
                    bool hasMore = VariableLengthUInt.DecodeByte((byte)bytes.Current, ref actualValue, ref numBitsDecoded);
                    if( !hasMore )
                        break;
                }
                if( bytes.MoveNext() )
                    Assert.Fail($"Fewer bytes expected!");

                Assert.AreEqual(expectedValue, actualValue);
            }
            void Test( uint value, byte[] encodedBytes )
            {
                TestEncoding(value, encodedBytes);
                TestDecoding(encodedBytes, value);
            }

            // test numbers taken from this wikipedia article (though our format is different!)
            // https://en.wikipedia.org/wiki/Variable-length_quantity#Examples
            Test(106903, new byte[] { 151, 195, 6 });
            Test(0, new byte[] { 0 });
            Test(127, new byte[] { 127 });
            Test(128, new byte[] { 128, 1 });
            Test(8192, new byte[] { 128, 64 });
            Test(16383, new byte[] { 255, 127 });
            Test(16384, new byte[] { 128, 128, 1 });
            Test(2097151, new byte[] { 255, 255, 127 });
            Test(2097152, new byte[] { 128, 128, 128, 1 });
            Test(134217728, new byte[] { 128, 128, 128, 64 });
            Test(268435455, new byte[] { 255, 255, 255, 127 });

            // some more Tests
            Test(int.MaxValue, new byte[] { 255, 255, 255, 255, 7 });
            Test(uint.MaxValue, new byte[] { 255, 255, 255, 255, 15 });
        }

        [Test]
        public static void SignedIntegerConversions()
        {
            Assert.AreEqual(uint.MaxValue, VariableLengthUInt.ToUnsigned(-1));
            Assert.AreEqual(-1, VariableLengthUInt.ToSigned(uint.MaxValue));

            Assert.AreEqual((uint)int.MaxValue + 1u, VariableLengthUInt.ToUnsigned(int.MinValue));
            Assert.AreEqual(int.MinValue, VariableLengthUInt.ToSigned((uint)int.MaxValue + 1u));
        }
    }
}
