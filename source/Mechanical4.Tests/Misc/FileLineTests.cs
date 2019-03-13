using System;
using Mechanical4.Misc;
using NUnit.Framework;

namespace Mechanical4.Tests.Misc
{
    [TestFixture]
    public static class FileLineTests
    {
        [Test]
        public static void EnglishDotNet()
        {
            Assert.True(
                string.Equals(
                    "  at Member in File.cs:line 5",
                    FileLine.EnglishDotNet(
                        "File.cs",
                        "Member",
                        5),
                    StringComparison.OrdinalIgnoreCase));

            Assert.True(
                string.Equals(
                    "  at Member in File.cs:line 5",
                    FileLine.EnglishDotNet(
                        @"c:\dir1\dir2\File.cs",
                        "Member",
                        5),
                    StringComparison.OrdinalIgnoreCase));

            Assert.True(
                string.Equals(
                    "  at Member in File.cs:line 5",
                    FileLine.EnglishDotNet(
                        @"/mnt/sdcard/File.cs",
                        "Member",
                        5),
                    StringComparison.OrdinalIgnoreCase));
        }

        [Test]
        public static void Compact()
        {
            Assert.True(
                string.Equals(
                    "Member, File.cs:5",
                    FileLine.Compact(
                        "File.cs",
                        "Member",
                        5),
                    StringComparison.OrdinalIgnoreCase));

            Assert.True(
                string.Equals(
                    "Member, File.cs:5",
                    FileLine.Compact(
                        @"c:\dir1\dir2\File.cs",
                        "Member",
                        5),
                    StringComparison.OrdinalIgnoreCase));

            Assert.True(
                string.Equals(
                    "Member, File.cs:5",
                    FileLine.Compact(
                        @"/mnt/sdcard/File.cs",
                        "Member",
                        5),
                    StringComparison.OrdinalIgnoreCase));
        }

        [Test]
        public static void ParseCompact()
        {
            FileLine.ParseCompact("Member, File.cs:5", out var file, out var member, out var line);
            Test.OrdinalEquals("File.cs", file);
            Test.OrdinalEquals("Member", member);
            Assert.AreEqual(5, line);

            FileLine.ParseCompact("Member,,Fi,:le::5", out file, out member, out line);
            Test.OrdinalEquals(",Fi,:le:", file);
            Test.OrdinalEquals("Member", member);
            Assert.AreEqual(5, line);
        }
    }
}
