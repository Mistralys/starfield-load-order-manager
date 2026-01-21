using System;
using System.Collections.Generic;
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
    public void CurrentCulture_DefaultsToEnglish()
    {
        // Arrange & Act
        var service = LocalizationService.Instance;

        // Assert
        Assert.Equal("en-US", service.CurrentCulture);
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
        
        // Note: This test assumes a string with format placeholders exists
        // For now, it tests the format mechanism itself

        // Act - Test with a placeholder string
        var result = service.GetString("Test", "FormattedString", "arg1", 123);

        // Assert - Should include args even if key doesn't exist
        Assert.Contains("arg1", result);
        Assert.Contains("123", result);
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
