using System;
using System.Globalization;
using System.Linq;
using Mechanical4.Core.Misc;
using NUnit.Framework;

namespace Mechanical4.Tests.Core.Misc
{
    [TestFixture]
    public static class CommonStringConverterTests
    {
        private static readonly CultureInfo English = new CultureInfo("en-US");
        private static readonly CultureInfo German = new CultureInfo("de-DE");

        private static void Converter_SingleTest<T, TConverter>( TConverter converter, T value, string str )
            where TConverter : IStringConverter<T>
        {
            Assert.NotNull(converter);

            string s = converter.ToString(value);
            Assert.True(string.Equals(str, s, StringComparison.Ordinal));

            T v;
            Assert.False(converter.TryParse(null, out v));
            Assert.True(converter.TryParse(s, out v));
            Assert.AreEqual(value, v);
        }

        private static void Converter_MultipleTests<T, TConverter>(
            CultureInfo culture,
            string format,
            NumberStyles style,
            (T value, string str)[] testItems )
            where TConverter : IStringConverter<T>
        {
            var converter = (TConverter)Activator.CreateInstance(typeof(TConverter), culture, format, style);
            foreach( var t in testItems )
                Converter_SingleTest(converter, t.value, t.str);

            T tmp;
            Assert.False(converter.TryParse(null, out tmp));
            Assert.False(converter.TryParse(string.Empty, out tmp));
            Assert.False(converter.TryParse(" ", out tmp));
            Assert.False(converter.TryParse("a", out tmp));
        }

        private static void IntegerTests<T, TConverter>()
            where TConverter : IStringConverter<T>
        {
            T minValue = (T)typeof(T).GetField("MinValue").GetValue(null);
            T maxValue = (T)typeof(T).GetField("MaxValue").GetValue(null);

            string toString( CultureInfo culture, string format, T value )
            {
                var method = typeof(T).GetMethod("ToString", new[] { typeof(string), typeof(IFormatProvider) });
                return (string)method.Invoke(value, new object[] { format, culture });
            }

            Converter_MultipleTests<T, TConverter>(
                English,
                "D",
                NumberStyles.AllowLeadingSign,
                new[] {
                    (minValue, toString(English, "D", minValue)),
                    (maxValue, toString(English, "D", maxValue)),
                    (default(T), "0")
                });

            Converter_MultipleTests<T, TConverter>(
                German,
                "C",
                NumberStyles.Currency,
                new[] {
                    (minValue, toString(German, "C", minValue)),
                    (maxValue, toString(German, "C", maxValue)),
                    (default(T), "0,00 €")
                });
        }

        private static void RationalTests<T, TConverter>()
            where TConverter : IStringConverter<T>
        {
            T minValue = (T)typeof(T).GetField("MinValue").GetValue(null);
            T maxValue = (T)typeof(T).GetField("MaxValue").GetValue(null);
            T negInfValue = default(T), posInfValue = default(T), nanValue = default(T), epsilonValue = default(T);
            if( typeof(T) != typeof(decimal) )
            {
                negInfValue = (T)typeof(T).GetField("NegativeInfinity").GetValue(null);
                posInfValue = (T)typeof(T).GetField("PositiveInfinity").GetValue(null);
                nanValue = (T)typeof(T).GetField("NaN").GetValue(null);
                epsilonValue = (T)typeof(T).GetField("Epsilon").GetValue(null);
            }

            string toString( CultureInfo culture, string format, T value )
            {
                var method = typeof(T).GetMethod("ToString", new[] { typeof(string), typeof(IFormatProvider) });
                return (string)method.Invoke(value, new object[] { format, culture });
            }

            string formatString = typeof(T) != typeof(decimal) ? "R" : "G";
            (T, string)[] testItems = new[]{
                (minValue, toString(English, formatString, minValue)),
                (maxValue, toString(English, formatString, maxValue)),
                (default(T), "0")
            };
            if( typeof(T) != typeof(decimal) )
            {
                testItems = testItems.Concat(new[]{
                    (negInfValue, toString(English, formatString, negInfValue)),
                    (posInfValue, toString(English, formatString, posInfValue)),
                    (nanValue, toString(English, formatString, nanValue)),
                    (epsilonValue, toString(English, formatString, epsilonValue)),
                })
                .ToArray();
            }
            Converter_MultipleTests<T, TConverter>(
                English,
                formatString,
                NumberStyles.Float,
                testItems);

            //// NOTE: we are unable to try with a different format string, since "R" is the only one can round-trip
        }

        private static T Parse<T, TConverter>( TConverter converter, string str )
            where TConverter : IStringConverter<T>
        {
            T result;
            if( converter.TryParse(str, out result) )
                return result;
            else
                throw new FormatException();
        }

        [Test]
        public static void SByteTests()
        {
            IntegerTests<sbyte, CommonStringConverters.SByte>();
        }

        [Test]
        public static void ByteTests()
        {
            IntegerTests<byte, CommonStringConverters.Byte>();
        }

        [Test]
        public static void Int16Tests()
        {
            IntegerTests<short, CommonStringConverters.Int16>();
        }

        [Test]
        public static void UInt16Tests()
        {
            IntegerTests<ushort, CommonStringConverters.UInt16>();
        }

        [Test]
        public static void Int32Tests()
        {
            IntegerTests<int, CommonStringConverters.Int32>();
        }

        [Test]
        public static void UInt32Tests()
        {
            IntegerTests<uint, CommonStringConverters.UInt32>();
        }

        [Test]
        public static void Int64Tests()
        {
            IntegerTests<long, CommonStringConverters.Int64>();
        }

        [Test]
        public static void UInt64Tests()
        {
            IntegerTests<ulong, CommonStringConverters.UInt64>();
        }

        [Test]
        public static void SingleTests()
        {
            RationalTests<float, CommonStringConverters.Single>();
        }

        [Test]
        public static void DoubleTests()
        {
            RationalTests<double, CommonStringConverters.Double>();
        }

        [Test]
        public static void DecimalTests()
        {
            RationalTests<decimal, CommonStringConverters.Decimal>();
        }

        [Test]
        public static void BooleanTests()
        {
            var converter = new CommonStringConverters.Boolean();
            Converter_SingleTest(converter, true, "true");
            Converter_SingleTest(converter, false, "false");

            Func<string, bool> parse = str => Parse<bool, CommonStringConverters.Boolean>(converter, str);
            Assert.AreEqual(true, parse("True"));
            Assert.AreEqual(true, parse("TRUE"));
            Assert.AreEqual(false, parse("falsE"));

            bool tmp;
            Assert.False(converter.TryParse(null, out tmp));
            Assert.False(converter.TryParse(string.Empty, out tmp));
            Assert.False(converter.TryParse(" ", out tmp));
            Assert.False(converter.TryParse("a", out tmp));

            Assert.False(converter.TryParse(" true", out tmp));
            Assert.False(converter.TryParse("tr ue", out tmp));
            Assert.False(converter.TryParse("true ", out tmp));
            Assert.False(converter.TryParse("fals", out tmp));
            Assert.False(converter.TryParse("alse", out tmp));
        }

        [Test]
        public static void CharTests()
        {
            var converter = new CommonStringConverters.Char();
            Converter_SingleTest(converter, 'C', "C");

            char tmp;
            Assert.False(converter.TryParse(null, out tmp));
            Assert.False(converter.TryParse(string.Empty, out tmp));
            Assert.False(converter.TryParse("xy", out tmp));
        }

        [Test]
        public static void StringTests()
        {
            var converter = new CommonStringConverters.String();
            Converter_SingleTest(converter, string.Empty, string.Empty);
            Converter_SingleTest(converter, " ", " ");
            Converter_SingleTest(converter, "a", "a");
            Converter_SingleTest(converter, "a b", "a b");

            string tmp;
            Assert.False(converter.TryParse(null, out tmp));
        }
    }
}
