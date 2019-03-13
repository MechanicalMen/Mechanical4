using System;
using NUnit.Framework;

namespace Mechanical4.Tests
{
    internal static class Test
    {
        private const int MaxStringSampleLength = 16;

        internal static void OrdinalEquals( string expected, string actual )
        {
            if( !string.Equals(expected, actual, StringComparison.Ordinal) )
                Assert.Fail(GetStringInequalityMessage(expected, actual, StringComparison.Ordinal));
        }

        private static string GetStringInequalityMessage( string expected, string actual, StringComparison comparison )
        {
            //// we can assume at this point, that there is some kind of character difference between the strings.

            if( string.IsNullOrEmpty(expected)
             && string.IsNullOrEmpty(actual) )
            {
                // both of them are null/empty
                if( ReferenceEquals(expected, null) )
                    return $"String inequality! Expected/Actual:\nnull\n\"\"";
                else
                    return $"String inequality! Expected/Actual:\n\"\"\nnull";
            }
            else if( string.IsNullOrEmpty(expected) != string.IsNullOrEmpty(actual) )
            {
                // one of them is null/empty, while the other is not
                if( ReferenceEquals(expected, null) )
                    return $"String inequality! Expected/Actual:\nnull\n\"{GetSubstringAroundMidpoint(actual, 0)}\"";
                else if( expected.Length == 0 )
                    return $"String inequality! Expected/Actual:\n\"\"\n\"{GetSubstringAroundMidpoint(actual, 0)}\"";
                else if( ReferenceEquals(actual, null) )
                    return $"String inequality! Expected/Actual:\n\"{GetSubstringAroundMidpoint(expected, 0)}\"\nnull";
                else
                    return $"String inequality! Expected/Actual:\n\"{GetSubstringAroundMidpoint(expected, 0)}\"\n\"\"";
            }
            else
            {
                // both are valid strings
                int midpoint = FindIndexOfFirstDifference(expected, actual, comparison);
                (var ln, var col) = ToLineCol(expected, midpoint);
                return $"String inequality! Expected/Actual:\n\"{GetSubstringAroundMidpoint(expected, midpoint)}\"\n\"{GetSubstringAroundMidpoint(actual, midpoint)}\"\nLn {ln}; Col {col}); Ch {midpoint})";
            }
        }

        private static string GetSubstringAroundMidpoint( string str, int midpointIndex )
        {
            if( string.IsNullOrEmpty(str) )
                return str;

            int startIndex = Math.Max(0, midpointIndex - (MaxStringSampleLength / 2));
            int length = Math.Min(str.Length - startIndex, MaxStringSampleLength);
            return str.Substring(startIndex, length)
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
        }

        private static int FindIndexOfFirstDifference( string str1, string str2, StringComparison comparison )
        {
            int overlapLength = Math.Min(str1?.Length ?? 0, str2?.Length ?? 0);
            for( int i = 1; i < overlapLength; ++i )
            {
                if( !string.Equals(str1.Substring(0, i), str2.Substring(0, i), comparison) )
                    return i - 1;
            }

            return overlapLength - 1;
        }

        private static (int line, int col) ToLineCol( string str, int index )
        {
            if( str == null )
                return (-1, -1);

            if( str.Length == 0 )
                return (1, 1);

            int ln = 1;
            int startIndex = 0;
            while( true )
            {
                int nextNewLineAt = str.IndexOf('\n', startIndex);
                if( nextNewLineAt == -1 )
                {
                    // no more new lines
                    break;
                }
                else
                {
                    // new line found
                    if( index > nextNewLineAt )
                    {
                        // new line before index
                        ln++;
                        startIndex = nextNewLineAt + 1;
                    }
                    else
                    {
                        // new line at or after index
                        break;
                    }
                }
            }

            return (ln, index - startIndex + 1);
        }
    }
}
