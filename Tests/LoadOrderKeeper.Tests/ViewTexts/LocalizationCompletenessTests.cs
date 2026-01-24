using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace LoadOrderKeeper.Tests.ViewTexts;

/// <summary>
/// Tests to ensure all locale files have complete translations with no missing keys.
/// This prevents untranslated text from appearing in the UI (e.g., [SectionKeyName]).
/// </summary>
public sealed class LocalizationCompletenessTests
{
    private readonly string _localesPath;
    private const string BaseLocale = "en-US";

    public LocalizationCompletenessTests()
    {
        // Get the locales path relative to the test assembly
        var testDir = AppDomain.CurrentDomain.BaseDirectory;
        _localesPath = Path.Combine(testDir, "ViewTexts", "Locales");
    }

    [Fact]
    public void LocalesDirectory_Exists()
    {
        // Arrange & Act & Assert
        Assert.True(Directory.Exists(_localesPath), 
            $"Locales directory not found at: {_localesPath}");
    }

    [Fact]
    public void BaseLocale_Exists()
    {
        // Arrange
        var baseLocalePath = Path.Combine(_localesPath, $"{BaseLocale}.json");

        // Act & Assert
        Assert.True(File.Exists(baseLocalePath), 
            $"Base locale file ({BaseLocale}.json) not found at: {baseLocalePath}");
    }

    [Fact]
    public void AllLocaleFiles_AreValidJson()
    {
        // Arrange
        var localeFiles = GetAllLocaleFiles();

        // Act & Assert
        foreach (var localeFile in localeFiles)
        {
            var json = File.ReadAllText(localeFile);
            var exception = Record.Exception(() => JsonDocument.Parse(json));
            
            Assert.True(exception == null, 
                $"Locale file is not valid JSON: {Path.GetFileName(localeFile)}\n" +
                $"Error: {exception?.Message}");
        }
    }

    [Fact]
    public void AllLocaleFiles_HaveLocaleName()
    {
        // Arrange
        var localeFiles = GetAllLocaleFiles();

        // Act & Assert
        foreach (var localeFile in localeFiles)
        {
            var json = File.ReadAllText(localeFile);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var hasLocaleName = root.TryGetProperty("LocaleName", out var localeNameElement);
            var localeName = localeNameElement.GetString();

            Assert.True(hasLocaleName, 
                $"Locale file missing 'LocaleName' property: {Path.GetFileName(localeFile)}");
            Assert.False(string.IsNullOrWhiteSpace(localeName), 
                $"Locale file has empty 'LocaleName': {Path.GetFileName(localeFile)}");
        }
    }

    [Fact]
    public void AllLocaleFiles_HaveParentCulture()
    {
        // Arrange
        var localeFiles = GetAllLocaleFiles();

        // Act & Assert
        foreach (var localeFile in localeFiles)
        {
            var json = File.ReadAllText(localeFile);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var hasParentCulture = root.TryGetProperty("ParentCulture", out var parentCultureElement);
            var parentCulture = parentCultureElement.GetString();

            Assert.True(hasParentCulture, 
                $"Locale file missing 'ParentCulture' property: {Path.GetFileName(localeFile)}");
            Assert.False(string.IsNullOrWhiteSpace(parentCulture), 
                $"Locale file has empty 'ParentCulture': {Path.GetFileName(localeFile)}");
        }
    }

    [Fact]
    public void AllLocaleFiles_HaveSameSections_AsBaseLocale()
    {
        // Arrange
        var baseKeys = GetAllKeys(BaseLocale);
        var baseSections = baseKeys.Select(k => k.Split('.')[0]).Distinct().OrderBy(s => s).ToList();
        var localeFiles = GetAllLocaleFiles().Where(f => !f.EndsWith($"{BaseLocale}.json")).ToList();

        // Act & Assert
        foreach (var localeFile in localeFiles)
        {
            var cultureName = Path.GetFileNameWithoutExtension(localeFile);
            var localeKeys = GetAllKeys(cultureName);
            var localeSections = localeKeys.Select(k => k.Split('.')[0]).Distinct().OrderBy(s => s).ToList();

            Assert.Equal(baseSections, localeSections);
        }
    }

    [Fact]
    public void AllLocaleFiles_HaveAllKeys_FromBaseLocale()
    {
        // Arrange
        var baseKeys = GetAllKeys(BaseLocale);
        var localeFiles = GetAllLocaleFiles().Where(f => !f.EndsWith($"{BaseLocale}.json")).ToList();

        // Act & Assert
        foreach (var localeFile in localeFiles)
        {
            var cultureName = Path.GetFileNameWithoutExtension(localeFile);
            var localeKeys = GetAllKeys(cultureName);
            var missingKeys = baseKeys.Except(localeKeys).OrderBy(k => k).ToList();

            Assert.True(missingKeys.Count == 0, 
                $"Locale file '{cultureName}.json' is missing {missingKeys.Count} key(s) from base locale:\n" +
                string.Join("\n", missingKeys.Select(k => $"  - {k}")));
        }
    }

    [Fact]
    public void AllLocaleFiles_HaveNoExtraKeys_NotInBaseLocale()
    {
        // Arrange
        var baseKeys = GetAllKeys(BaseLocale);
        var localeFiles = GetAllLocaleFiles().Where(f => !f.EndsWith($"{BaseLocale}.json")).ToList();

        // Act & Assert
        foreach (var localeFile in localeFiles)
        {
            var cultureName = Path.GetFileNameWithoutExtension(localeFile);
            var localeKeys = GetAllKeys(cultureName);
            var extraKeys = localeKeys.Except(baseKeys).OrderBy(k => k).ToList();

            Assert.True(extraKeys.Count == 0, 
                $"Locale file '{cultureName}.json' has {extraKeys.Count} extra key(s) not present in base locale:\n" +
                string.Join("\n", extraKeys.Select(k => $"  - {k}")));
        }
    }

    [Fact]
    public void AllLocaleFiles_HaveNoEmptyValues()
    {
        // Arrange
        var localeFiles = GetAllLocaleFiles();

        // Act & Assert
        foreach (var localeFile in localeFiles)
        {
            var cultureName = Path.GetFileNameWithoutExtension(localeFile);
            var emptyValues = GetKeysWithEmptyValues(cultureName);

            Assert.Empty(emptyValues);
        }
    }

    [Fact]
    public void AllLocaleFiles_PreserveFormatPlaceholders()
    {
        // Arrange
        var baseKeysWithPlaceholders = GetKeysWithFormatPlaceholders(BaseLocale);
        var localeFiles = GetAllLocaleFiles().Where(f => !f.EndsWith($"{BaseLocale}.json")).ToList();

        // Act & Assert
        foreach (var localeFile in localeFiles)
        {
            var cultureName = Path.GetFileNameWithoutExtension(localeFile);

            foreach (var (key, basePlaceholders) in baseKeysWithPlaceholders)
            {
                var localePlaceholders = GetFormatPlaceholders(cultureName, key);
                
                Assert.Equal(basePlaceholders.Count, localePlaceholders.Count);
                Assert.Equal(basePlaceholders.OrderBy(p => p), localePlaceholders.OrderBy(p => p));
            }
        }
    }

    #region Helper Methods

    private List<string> GetAllLocaleFiles()
    {
        if (!Directory.Exists(_localesPath))
        {
            return new List<string>();
        }

        return Directory.GetFiles(_localesPath, "*.json")
            .OrderBy(f => f)
            .ToList();
    }

    private HashSet<string> GetAllKeys(string cultureName)
    {
        var filePath = Path.Combine(_localesPath, $"{cultureName}.json");
        var keys = new HashSet<string>();

        if (!File.Exists(filePath))
        {
            return keys;
        }

        var json = File.ReadAllText(filePath);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        foreach (var section in root.EnumerateObject())
        {
            var sectionName = section.Name;

            // Skip metadata properties (not translation sections)
            if (section.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var entry in section.Value.EnumerateObject())
            {
                var key = $"{sectionName}.{entry.Name}";
                keys.Add(key);
            }
        }

        return keys;
    }

    private List<string> GetKeysWithEmptyValues(string cultureName)
    {
        var filePath = Path.Combine(_localesPath, $"{cultureName}.json");
        var emptyKeys = new List<string>();

        if (!File.Exists(filePath))
        {
            return emptyKeys;
        }

        var json = File.ReadAllText(filePath);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        foreach (var section in root.EnumerateObject())
        {
            var sectionName = section.Name;

            // Skip metadata properties
            if (section.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var entry in section.Value.EnumerateObject())
            {
                var value = entry.Value.GetString();
                if (string.IsNullOrWhiteSpace(value))
                {
                    emptyKeys.Add($"{sectionName}.{entry.Name}");
                }
            }
        }

        return emptyKeys;
    }

    private Dictionary<string, List<string>> GetKeysWithFormatPlaceholders(string cultureName)
    {
        var filePath = Path.Combine(_localesPath, $"{cultureName}.json");
        var keysWithPlaceholders = new Dictionary<string, List<string>>();

        if (!File.Exists(filePath))
        {
            return keysWithPlaceholders;
        }

        var json = File.ReadAllText(filePath);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        foreach (var section in root.EnumerateObject())
        {
            var sectionName = section.Name;

            // Skip metadata properties
            if (section.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var entry in section.Value.EnumerateObject())
            {
                var value = entry.Value.GetString() ?? string.Empty;
                var placeholders = ExtractFormatPlaceholders(value);

                if (placeholders.Count > 0)
                {
                    var key = $"{sectionName}.{entry.Name}";
                    keysWithPlaceholders[key] = placeholders;
                }
            }
        }

        return keysWithPlaceholders;
    }

    private List<string> GetFormatPlaceholders(string cultureName, string fullKey)
    {
        var parts = fullKey.Split('.');
        if (parts.Length < 2)
        {
            return new List<string>();
        }

        var sectionName = parts[0];
        var keyName = string.Join(".", parts.Skip(1));

        var filePath = Path.Combine(_localesPath, $"{cultureName}.json");

        if (!File.Exists(filePath))
        {
            return new List<string>();
        }

        var json = File.ReadAllText(filePath);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty(sectionName, out var section) && 
            section.TryGetProperty(keyName, out var entry))
        {
            var value = entry.GetString() ?? string.Empty;
            return ExtractFormatPlaceholders(value);
        }

        return new List<string>();
    }

    private List<string> ExtractFormatPlaceholders(string text)
    {
        var placeholders = new List<string>();
        var startIndex = 0;

        while (true)
        {
            startIndex = text.IndexOf('{', startIndex);
            if (startIndex == -1)
            {
                break;
            }

            var endIndex = text.IndexOf('}', startIndex);
            if (endIndex == -1)
            {
                break;
            }

            var placeholder = text.Substring(startIndex, endIndex - startIndex + 1);
            placeholders.Add(placeholder);
            startIndex = endIndex + 1;
        }

        return placeholders;
    }

    #endregion
}
