using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LoadOrderKeeper.Helpers;
using Xunit;

namespace LoadOrderKeeper.Tests.Helpers;

public class SteamLibraryVdfParserTests
{
    [Fact]
    public void Parse_LoadsCopiedFixtureAndPreservesLibraryOrder()
    {
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "example-steam-library.vdf");

        Assert.True(File.Exists(fixturePath));

        var entries = SteamLibraryVdfParser.Parse(File.ReadAllText(fixturePath));

        Assert.Equal(new[] { @"C:\Steam", @"D:\SteamLibrary", @"E:\SteamLibrary", @"F:\SteamLibrary" }, entries.Select(entry => entry.Path));
        Assert.NotNull(entries[0].AppIds);
        Assert.Contains("1716740", entries[0].AppIds!);
        Assert.NotNull(entries[3].AppIds);
        Assert.Empty(entries[3].AppIds!);
    }

    [Fact]
    public void Parse_DecodesSupportedEscapesAndAcceptsComments()
    {
        const string content = """
            // Leading comment
            "libraryfolders" // Root comment
            {
                "0"
                {
                    "path" "C:\\Games\\Steam\"Library\n\r\t"
                    "apps" // Inter-token comment
                    {
                        "1716740" "1"
                    }
                }
            }
            """;

        var entry = Assert.Single(SteamLibraryVdfParser.Parse(content));

        Assert.Equal("C:\\Games\\Steam\"Library\n\r\t", entry.Path);
        Assert.NotNull(entry.AppIds);
        Assert.Contains("1716740", entry.AppIds!);
    }

    [Fact]
    public void Parse_UsesNullableSectionsAndIgnoresScalarRootChildren()
    {
        const string content = """
            "libraryfolders"
            {
                "metadata" "ignored"
                "0" { "apps" { } }
                "1" { "path" "D:\\Steam" }
                "2" { "path" "E:\\Steam" "apps" "not-an-object" }
                "3" { "path" "F:\\Steam" "apps" { } }
            }
            """;

        var entries = SteamLibraryVdfParser.Parse(content);

        Assert.Equal(4, entries.Count);
        Assert.Null(entries[0].Path);
        Assert.Empty(entries[0].AppIds!);
        Assert.Equal(@"D:\Steam", entries[1].Path);
        Assert.Null(entries[1].AppIds);
        Assert.Equal(@"E:\Steam", entries[2].Path);
        Assert.Null(entries[2].AppIds);
        Assert.Equal(@"F:\Steam", entries[3].Path);
        Assert.Empty(entries[3].AppIds!);
    }

    public static IEnumerable<object[]> InvalidDocuments =>
    [
        [""],
        ["\"libraryfolders\" {} \"extra\" {}"],
        ["\"libraryfolders\" { \"0\" { \"path\" \"a\" \"path\" \"b\" } }"],
        ["\"libraryfolders\" { \"0\" }"],
        ["\"libraryfolders\" { \"0\" { \"path\" \"C:\\q\" } }"],
        [""" "libraryfolders" { "0" { "path" "C:\" } } """],
        ["\"libraryfolders\" { \"0\" { \"path\" \"unterminated } }"],
        ["\"libraryfolders\" { \"0\" { \"path\" \"a\" }"],
        ["\"libraryfolders\" { ] }"],
    ];

    [Theory]
    [MemberData(nameof(InvalidDocuments))]
    public void Parse_RejectsMalformedInput(string content)
    {
        Assert.Throws<FormatException>(() => SteamLibraryVdfParser.Parse(content));
    }
}