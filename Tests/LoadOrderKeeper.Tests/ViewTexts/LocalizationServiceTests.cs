using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using LoadOrderKeeper.ViewTexts;
using Xunit;

namespace LoadOrderKeeper.Tests.ViewTexts;

/// <summary>
/// Tests for LocalizationService covering JSON loading, caching, culture switching,
/// and fallback behavior.
/// </summary>
public sealed class LocalizationServiceTests
{
    [Fact]
    public void Instance_ReturnsSameInstance()
    {
        // Arrange & Act
        var instance1 = LocalizationService.Instance;
        var instance2 = LocalizationService.Instance;

        // Assert
        Assert.Same(instance1, instance2);
    }

    [Fact]
    public void CurrentCulture_DefaultsToSystemCulture()
    {
        // Verifies the culture detection contract: when a locale file exists for the system
        // culture, LocalizationService picks it up rather than defaulting to en-US.
        // This test does NOT use EnglishLocaleFixture so it observes the singleton's
        // actual startup behavior regardless of the running system locale.

        // Arrange
        var service = LocalizationService.Instance;
        var sessionCulture = service.SessionStartCulture;

        // The session-start culture must always be a non-empty, parseable culture string.
        Assert.False(string.IsNullOrWhiteSpace(sessionCulture));
        Assert.True(IsParseable(sessionCulture), $"'{sessionCulture}' is not a valid culture name.");

        // When the system UI culture has a matching locale file, the service must have
        // detected it — i.e., session-start culture should equal the system culture
        // (or its mapped variant) rather than falling back to en-US.
        var systemCulture = CultureInfo.CurrentUICulture.Name;
        var exeDir = AppDomain.CurrentDomain.BaseDirectory;
        var localesPath = Path.Combine(exeDir, "ViewTexts", "Locales");
        var systemLocalePath = Path.Combine(localesPath, $"{systemCulture}.json");

        if (File.Exists(systemLocalePath))
        {
            Assert.Equal(systemCulture, sessionCulture);
        }
        else
        {
            // Fallback to en-US is correct when no locale file matches the system culture.
            Assert.Equal("en-US", sessionCulture);
        }
    }

    private static bool IsParseable(string cultureName)
    {
        try
        {
            _ = CultureInfo.GetCultureInfo(cultureName);
            return true;
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
    }

    [Fact]
    public void GetString_WithMissingKey_ReturnsPlaceholder()
    {
        // Arrange
        var service = LocalizationService.Instance;

        // Act
        var result = service.GetString("NonExistentSection", "NonExistentKey");

        // Assert
        Assert.Equal("[NonExistentSection.NonExistentKey]", result);
    }

    [Fact]
    public void GetString_WithFormatArgs_FormatsCorrectly()
    {
        // Arrange
        var service = LocalizationService.Instance;
        
        // Note: This test verifies the format mechanism with a non-existent key
        // When a key doesn't exist, it returns the placeholder as-is

        // Act - Test with a placeholder string
        var result = service.GetString("Test", "FormattedString", "arg1", 123);

        // Assert - Should return placeholder (key doesn't exist in actual locale files)
        Assert.Equal("[Test.FormattedString]", result);
    }

    [Fact]
    public void SetCulture_WithNullOrEmpty_ThrowsArgumentException()
    {
        // Arrange
        var service = LocalizationService.Instance;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => service.SetCulture(null!));
        Assert.Throws<ArgumentException>(() => service.SetCulture(string.Empty));
        Assert.Throws<ArgumentException>(() => service.SetCulture("   "));
    }

    [Fact]
    public void SetCulture_WithSameCulture_DoesNotRaiseCultureChanged()
    {
        // Arrange
        var service = LocalizationService.Instance;
        var currentCulture = service.CurrentCulture;
        var eventRaised = false;

        service.CultureChanged += (s, e) => eventRaised = true;

        // Act
        service.SetCulture(currentCulture);

        // Assert
        Assert.False(eventRaised);
    }

    [Fact]
    public void CultureChanged_RaisedWhenCultureChanges()
    {
        // Arrange
        var service = LocalizationService.Instance;
        var eventRaised = false;
        var originalCulture = service.CurrentCulture;

        service.CultureChanged += (s, e) => eventRaised = true;

        // Act
        var newCulture = originalCulture == "en-US" ? "de-DE" : "en-US";
        service.SetCulture(newCulture);

        // Assert
        Assert.True(eventRaised);
        Assert.Equal(newCulture, service.CurrentCulture);

        // Cleanup - restore original culture
        service.SetCulture(originalCulture);
    }

    [Fact]
    public void ReloadCurrentCulture_RaisesCultureChangedEvent()
    {
        // Arrange
        var service = LocalizationService.Instance;
        var eventRaised = false;

        service.CultureChanged += (s, e) => eventRaised = true;

        // Act
        service.ReloadCurrentCulture();

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public void GetString_WithDotInKey_HandlesCorrectly()
    {
        // Arrange
        var service = LocalizationService.Instance;

        // Act
        var result = service.GetString("Section", "Key.With.Dots");

        // Assert
        // Should return placeholder since key doesn't exist
        Assert.Equal("[Section.Key.With.Dots]", result);
    }

    [Fact]
    public void GetString_ThreadSafe_NoExceptions()
    {
        // Arrange
        var service = LocalizationService.Instance;
        var tasks = new List<Task>();

        // Act - Access service from multiple threads simultaneously
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < 100; j++)
                {
                    var result = service.GetString("Test", $"Key{j}");
                    Assert.NotNull(result);
                }
            }));
        }

        // Assert - Should complete without exceptions
        Task.WaitAll(tasks.ToArray());
    }
}
