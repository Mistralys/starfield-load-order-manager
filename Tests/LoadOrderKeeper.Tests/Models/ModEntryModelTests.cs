using System;
using LoadOrderKeeper.Models;
using Xunit;

namespace LoadOrderKeeper.Tests.Models;

/// <summary>
/// Tests for ModEntryModel covering line parsing, enabled/disabled detection,
/// file name extraction, serialization, and equality comparison.
/// </summary>
public sealed class ModEntryModelTests
{
    #region Constructor and Parsing Tests

    [Fact]
    public void Constructor_EnabledMod_ParsesCorrectly()
    {
        // Arrange
        var line = "*Skyrim.esm";

        // Act
        var entry = new ModEntryModel(line);

        // Assert
        Assert.True(entry.IsEnabled);
        Assert.Equal("Skyrim.esm", entry.FileName);
    }

    [Fact]
    public void Constructor_DisabledMod_ParsesCorrectly()
    {
        // Arrange
        var line = "Skyrim.esm";

        // Act
        var entry = new ModEntryModel(line);

        // Assert
        Assert.False(entry.IsEnabled);
        Assert.Equal("Skyrim.esm", entry.FileName);
    }

    [Fact]
    public void Constructor_LineWithSpaces_TrimsCorrectly()
    {
        // Arrange
        var line = "  *Skyrim.esm  ";

        // Act
        var entry = new ModEntryModel(line);

        // Assert
        Assert.True(entry.IsEnabled);
        Assert.Equal("Skyrim.esm", entry.FileName);
    }

    [Fact]
    public void Constructor_LineWithSpacesAfterAsterisk_TrimsFileName()
    {
        // Arrange
        var line = "*  Skyrim.esm  ";

        // Act
        var entry = new ModEntryModel(line);

        // Assert
        Assert.True(entry.IsEnabled);
        Assert.Equal("Skyrim.esm", entry.FileName);
    }

    [Fact]
    public void Constructor_EmptyString_CreatesEntryWithEmptyFileName()
    {
        // Arrange
        var line = "";

        // Act
        var entry = new ModEntryModel(line);

        // Assert
        Assert.False(entry.IsEnabled);
        Assert.Equal(string.Empty, entry.FileName);
    }

    [Fact]
    public void Constructor_NullString_CreatesEntryWithEmptyFileName()
    {
        // Arrange
        string? line = null;

        // Act
        var entry = new ModEntryModel(line!);

        // Assert
        Assert.False(entry.IsEnabled);
        Assert.Equal(string.Empty, entry.FileName);
    }

    [Fact]
    public void Constructor_OnlyAsterisk_CreatesEnabledEntryWithEmptyFileName()
    {
        // Arrange
        var line = "*";

        // Act
        var entry = new ModEntryModel(line);

        // Assert
        Assert.True(entry.IsEnabled);
        Assert.Equal(string.Empty, entry.FileName);
    }

    #endregion

    #region Line Number Tests

    [Fact]
    public void Constructor_WithLineNumber_SetsLineNumber()
    {
        // Arrange
        var line = "*Skyrim.esm";

        // Act
        var entry = new ModEntryModel(line, lineNumber: 5);

        // Assert
        Assert.Equal(5, entry.LineNumber);
    }

    [Fact]
    public void Constructor_WithoutLineNumber_LineNumberIsNull()
    {
        // Arrange
        var line = "*Skyrim.esm";

        // Act
        var entry = new ModEntryModel(line);

        // Assert
        Assert.Null(entry.LineNumber);
    }

    [Fact]
    public void Constructor_WithOriginalLineNumber_SetsOriginalLineNumber()
    {
        // Arrange
        var line = "*Skyrim.esm";

        // Act
        var entry = new ModEntryModel(line, lineNumber: 5, originalLineNumber: 3);

        // Assert
        Assert.Equal(3, entry.OriginalLineNumber);
    }

    [Fact]
    public void Constructor_WithoutOriginalLineNumber_CopiesFromLineNumber()
    {
        // Arrange
        var line = "*Skyrim.esm";

        // Act
        var entry = new ModEntryModel(line, lineNumber: 5);

        // Assert
        Assert.Equal(5, entry.OriginalLineNumber);
    }

    [Fact]
    public void LineNumber_CanBeModified()
    {
        // Arrange
        var entry = new ModEntryModel("*Skyrim.esm", lineNumber: 1);

        // Act
        entry.LineNumber = 10;

        // Assert
        Assert.Equal(10, entry.LineNumber);
    }

    [Fact]
    public void OriginalLineNumber_CanBeModified()
    {
        // Arrange
        var entry = new ModEntryModel("*Skyrim.esm", lineNumber: 1);

        // Act
        entry.OriginalLineNumber = 5;

        // Assert
        Assert.Equal(5, entry.OriginalLineNumber);
    }

    #endregion

    #region ToLine Tests

    [Fact]
    public void ToLine_ReturnsFormattedLine()
    {
        // Arrange
        var entry = new ModEntryModel("*Skyrim.esm");

        // Act
        var line = entry.ToLine();

        // Assert
        Assert.Equal("*Skyrim.esm", line);
    }

    [Fact]
    public void ToLine_DisabledMod_StillReturnsEnabledFormat()
    {
        // Arrange
        var entry = new ModEntryModel("Skyrim.esm"); // Disabled

        // Act
        var line = entry.ToLine();

        // Assert
        // ToLine() always returns enabled format
        Assert.Equal("*Skyrim.esm", line);
    }

    [Fact]
    public void ToLine_EmptyFileName_ReturnsAsteriskOnly()
    {
        // Arrange
        var entry = new ModEntryModel("");

        // Act
        var line = entry.ToLine();

        // Assert
        Assert.Equal("*", line);
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_ReturnsSameAsToLine()
    {
        // Arrange
        var entry = new ModEntryModel("*Skyrim.esm");

        // Act
        var toStringResult = entry.ToString();
        var toLineResult = entry.ToLine();

        // Assert
        Assert.Equal(toLineResult, toStringResult);
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_SameFileName_ReturnsTrue()
    {
        // Arrange
        var entry1 = new ModEntryModel("*Skyrim.esm");
        var entry2 = new ModEntryModel("*Skyrim.esm");

        // Act & Assert
        Assert.True(entry1.Equals(entry2));
    }

    [Fact]
    public void Equals_DifferentFileNames_ReturnsFalse()
    {
        // Arrange
        var entry1 = new ModEntryModel("*Skyrim.esm");
        var entry2 = new ModEntryModel("*Fallout4.esm");

        // Act & Assert
        Assert.False(entry1.Equals(entry2));
    }

    [Fact]
    public void Equals_CaseInsensitive()
    {
        // Arrange
        var entry1 = new ModEntryModel("*Skyrim.esm");
        var entry2 = new ModEntryModel("*skyrim.esm");

        // Act & Assert
        Assert.True(entry1.Equals(entry2));
    }

    [Fact]
    public void Equals_DifferentLineNumbers_StillEqual()
    {
        // Arrange
        var entry1 = new ModEntryModel("*Skyrim.esm", lineNumber: 1);
        var entry2 = new ModEntryModel("*Skyrim.esm", lineNumber: 10);

        // Act & Assert
        // Equality is based on file name only, not line numbers
        Assert.True(entry1.Equals(entry2));
    }

    [Fact]
    public void Equals_EnabledVsDisabled_StillEqual()
    {
        // Arrange
        var entry1 = new ModEntryModel("*Skyrim.esm");
        var entry2 = new ModEntryModel("Skyrim.esm"); // Disabled

        // Act & Assert
        // Equality is based on file name only
        Assert.True(entry1.Equals(entry2));
    }

    [Fact]
    public void Equals_NullObject_ReturnsFalse()
    {
        // Arrange
        var entry = new ModEntryModel("*Skyrim.esm");

        // Act & Assert
        Assert.False(entry.Equals((ModEntryModel?)null));
    }

    [Fact]
    public void Equals_ObjectOverload_WorksCorrectly()
    {
        // Arrange
        var entry1 = new ModEntryModel("*Skyrim.esm");
        object entry2 = new ModEntryModel("*Skyrim.esm");

        // Act & Assert
        Assert.True(entry1.Equals(entry2));
    }

    [Fact]
    public void Equals_DifferentType_ReturnsFalse()
    {
        // Arrange
        var entry = new ModEntryModel("*Skyrim.esm");
        object other = "Skyrim.esm";

        // Act & Assert
        Assert.False(entry.Equals(other));
    }

    #endregion

    #region GetHashCode Tests

    [Fact]
    public void GetHashCode_SameFileName_SameHashCode()
    {
        // Arrange
        var entry1 = new ModEntryModel("*Skyrim.esm");
        var entry2 = new ModEntryModel("*Skyrim.esm");

        // Act
        var hash1 = entry1.GetHashCode();
        var hash2 = entry2.GetHashCode();

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void GetHashCode_CaseInsensitive()
    {
        // Arrange
        var entry1 = new ModEntryModel("*Skyrim.esm");
        var entry2 = new ModEntryModel("*skyrim.esm");

        // Act
        var hash1 = entry1.GetHashCode();
        var hash2 = entry2.GetHashCode();

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void GetHashCode_DifferentFileNames_DifferentHashCodes()
    {
        // Arrange
        var entry1 = new ModEntryModel("*Skyrim.esm");
        var entry2 = new ModEntryModel("*Fallout4.esm");

        // Act
        var hash1 = entry1.GetHashCode();
        var hash2 = entry2.GetHashCode();

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    #endregion

    #region HashSet Compatibility Tests

    [Fact]
    public void HashSetContains_FindsEqualEntry()
    {
        // Arrange
        var entry1 = new ModEntryModel("*Skyrim.esm", lineNumber: 1);
        var entry2 = new ModEntryModel("*Skyrim.esm", lineNumber: 10);
        var set = new System.Collections.Generic.HashSet<ModEntryModel> { entry1 };

        // Act & Assert
        Assert.Contains(entry2, set);
    }

    [Fact]
    public void HashSetContains_CaseInsensitive()
    {
        // Arrange
        var entry1 = new ModEntryModel("*Skyrim.esm");
        var entry2 = new ModEntryModel("*skyrim.esm");
        var set = new System.Collections.Generic.HashSet<ModEntryModel> { entry1 };

        // Act & Assert
        Assert.Contains(entry2, set);
    }

    #endregion

    #region Special Characters and Edge Cases Tests

    [Fact]
    public void Constructor_FileNameWithSpecialCharacters_ParsesCorrectly()
    {
        // Arrange
        var line = "*Mod (Version 1.2).esp";

        // Act
        var entry = new ModEntryModel(line);

        // Assert
        Assert.Equal("Mod (Version 1.2).esp", entry.FileName);
    }

    [Fact]
    public void Constructor_FileNameWithMultipleSpaces_TrimsCorrectly()
    {
        // Arrange
        var line = "*  Multiple   Spaces.esp  ";

        // Act
        var entry = new ModEntryModel(line);

        // Assert
        // Should trim leading/trailing spaces but preserve internal spaces
        Assert.Contains("Spaces.esp", entry.FileName);
    }

    [Fact]
    public void Constructor_UnicodeFileName_PreservesCharacters()
    {
        // Arrange
        var line = "*???????????.esp";

        // Act
        var entry = new ModEntryModel(line);

        // Assert
        Assert.Equal("???????????.esp", entry.FileName);
    }

    #endregion
}
