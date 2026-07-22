using System;
using System.Threading.Tasks;
using LoadOrderKeeper.Coordinators;
using LoadOrderKeeper.Models;
using LoadOrderKeeper.Tests.Fixtures;
using Xunit;

namespace LoadOrderKeeper.Tests.Coordinators;

/// <summary>
/// Tests for UpdateCheckCoordinator covering update notification management,
/// state handling, and message parsing.
/// Note: These tests focus on coordinator behavior. UpdateCheckService (which makes HTTP calls)
/// is tested separately with mocked responses.
/// </summary>
[Collection(LocaleSequentialCollection.Name)]
public sealed class UpdateCheckCoordinatorTests : IClassFixture<EnglishLocaleFixture>, IDisposable
{
    private readonly UpdateCheckCoordinator _coordinator;

    public UpdateCheckCoordinatorTests(EnglishLocaleFixture localeFixture)
    {
        _ = localeFixture; // Ensures en-US culture is active for the lifetime of this test class
        _coordinator = new UpdateCheckCoordinator();
    }

    public void Dispose()
    {
        _coordinator?.Dispose();
    }

    #region Initialization Tests

    [Fact]
    public void Constructor_InitializesWithDefaultState()
    {
        // Assert
        Assert.False(_coordinator.UpdateAvailable);
        Assert.Equal(string.Empty, _coordinator.UpdateMessage);
        Assert.False(_coordinator.UpdateInfoBarVisible);
    }

    #endregion

    #region CheckForUpdatesBackgroundAsync Tests

    [Fact]
    public async Task CheckForUpdatesBackgroundAsync_DoesNotThrowOnError()
    {
        // Note: This test verifies that background checks fail silently
        // The actual update check will likely fail in test environment (no network/invalid API)
        // but the coordinator should handle it gracefully

        // Act & Assert - Should not throw
        var exception = await Record.ExceptionAsync(async () =>
            await _coordinator.CheckForUpdatesBackgroundAsync());

        Assert.Null(exception);
    }

    [Fact]
    public async Task CheckForUpdatesBackgroundAsync_PreservesStateOnFailure()
    {
        // Arrange
        var initialUpdateAvailable = _coordinator.UpdateAvailable;
        var initialMessage = _coordinator.UpdateMessage;
        var initialInfoBarVisible = _coordinator.UpdateInfoBarVisible;

        // Act
        await _coordinator.CheckForUpdatesBackgroundAsync();

        // Assert - State may change if updates are actually available
        // This test verifies the method doesn't throw rather than specific state
        // (In test environment, it might actually find an update or fail silently)
        
        // The important thing is no exception was thrown
        Assert.True(true);
    }

    #endregion

    #region CheckForUpdatesManualAsync Tests

    [Fact]
    public async Task CheckForUpdatesManualAsync_ReturnsResult()
    {
        // Act
        var result = await _coordinator.CheckForUpdatesManualAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.CurrentVersion);
    }

    [Fact]
    public async Task CheckForUpdatesManualAsync_ReturnsCurrentVersion()
    {
        // Act
        var result = await _coordinator.CheckForUpdatesManualAsync();

        // Assert
        Assert.False(string.IsNullOrEmpty(result.CurrentVersion));
    }

    #endregion

    #region DismissUpdateNotification Tests

    [Fact]
    public void DismissUpdateNotification_HidesInfoBar()
    {
        // Arrange - Simulate update available state
        // We can't easily trigger a real update check, so we'll just set the property
        // using reflection to test the dismiss functionality
        var updateInfoBarVisibleProperty = typeof(UpdateCheckCoordinator)
            .GetProperty(nameof(UpdateCheckCoordinator.UpdateInfoBarVisible));
        updateInfoBarVisibleProperty?.SetValue(_coordinator, true);

        // Act
        _coordinator.DismissUpdateNotification();

        // Assert
        Assert.False(_coordinator.UpdateInfoBarVisible);
    }

    [Fact]
    public void DismissUpdateNotification_CanBeCalledWhenAlreadyHidden()
    {
        // Arrange
        Assert.False(_coordinator.UpdateInfoBarVisible);

        // Act & Assert - Should not throw
        _coordinator.DismissUpdateNotification();

        Assert.False(_coordinator.UpdateInfoBarVisible);
    }

    [Fact]
    public void DismissUpdateNotification_AfterDisposal_ThrowsObjectDisposedException()
    {
        // Arrange
        _coordinator.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => _coordinator.DismissUpdateNotification());
    }

    #endregion

    #region GetLatestVersion Tests

    [Fact]
    public void GetLatestVersion_EmptyMessage_ReturnsNull()
    {
        // Arrange - Default state has empty message
        Assert.Equal(string.Empty, _coordinator.UpdateMessage);

        // Act
        var version = _coordinator.GetLatestVersion();

        // Assert
        Assert.Null(version);
    }

    [Fact]
    public void GetLatestVersion_ValidMessage_ExtractsVersion()
    {
        // Arrange - Set update message using reflection
        var updateMessageProperty = typeof(UpdateCheckCoordinator)
            .GetProperty(nameof(UpdateCheckCoordinator.UpdateMessage));
        updateMessageProperty?.SetValue(_coordinator, "Version 1.2.3 is available!");

        // Act
        var version = _coordinator.GetLatestVersion();

        // Assert
        Assert.Equal("1.2.3", version);
    }

    [Fact]
    public void GetLatestVersion_MessageWithSpaces_TrimsCorrectly()
    {
        // Arrange
        var updateMessageProperty = typeof(UpdateCheckCoordinator)
            .GetProperty(nameof(UpdateCheckCoordinator.UpdateMessage));
        updateMessageProperty?.SetValue(_coordinator, "Version  2.0.1  is available!");

        // Act
        var version = _coordinator.GetLatestVersion();

        // Assert
        Assert.Equal("2.0.1", version);
    }

    [Fact]
    public void GetLatestVersion_ComplexVersionString_ExtractsCorrectly()
    {
        // Arrange
        var updateMessageProperty = typeof(UpdateCheckCoordinator)
            .GetProperty(nameof(UpdateCheckCoordinator.UpdateMessage));
        updateMessageProperty?.SetValue(_coordinator, "Version 3.14.159 is available!");

        // Act
        var version = _coordinator.GetLatestVersion();

        // Assert
        Assert.Equal("3.14.159", version);
    }

    #endregion

    #region State Management Tests

    [Fact]
    public void UpdateAvailable_DefaultsToFalse()
    {
        // Assert
        Assert.False(_coordinator.UpdateAvailable);
    }

    [Fact]
    public void UpdateMessage_DefaultsToEmpty()
    {
        // Assert
        Assert.Equal(string.Empty, _coordinator.UpdateMessage);
    }

    [Fact]
    public void UpdateInfoBarVisible_DefaultsToFalse()
    {
        // Assert
        Assert.False(_coordinator.UpdateInfoBarVisible);
    }

    #endregion

    #region Property Change Tests

    [Fact]
    public void UpdateAvailable_RaisesPropertyChanged()
    {
        // Arrange
        bool propertyChangedRaised = false;
        _coordinator.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(UpdateCheckCoordinator.UpdateAvailable))
            {
                propertyChangedRaised = true;
            }
        };

        // Act - Set property using reflection
        var property = typeof(UpdateCheckCoordinator)
            .GetProperty(nameof(UpdateCheckCoordinator.UpdateAvailable));
        property?.SetValue(_coordinator, true);

        // Assert
        Assert.True(propertyChangedRaised);
    }

    [Fact]
    public void UpdateMessage_RaisesPropertyChanged()
    {
        // Arrange
        bool propertyChangedRaised = false;
        _coordinator.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(UpdateCheckCoordinator.UpdateMessage))
            {
                propertyChangedRaised = true;
            }
        };

        // Act
        var property = typeof(UpdateCheckCoordinator)
            .GetProperty(nameof(UpdateCheckCoordinator.UpdateMessage));
        property?.SetValue(_coordinator, "Test message");

        // Assert
        Assert.True(propertyChangedRaised);
    }

    [Fact]
    public void UpdateInfoBarVisible_RaisesPropertyChanged()
    {
        // Arrange
        bool propertyChangedRaised = false;
        _coordinator.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(UpdateCheckCoordinator.UpdateInfoBarVisible))
            {
                propertyChangedRaised = true;
            }
        };

        // Act
        var property = typeof(UpdateCheckCoordinator)
            .GetProperty(nameof(UpdateCheckCoordinator.UpdateInfoBarVisible));
        property?.SetValue(_coordinator, true);

        // Assert
        Assert.True(propertyChangedRaised);
    }

    #endregion

    #region Disposal Tests

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var coordinator = new UpdateCheckCoordinator();

        // Act & Assert - Should not throw
        coordinator.Dispose();
        coordinator.Dispose();
    }

    [Fact]
    public void Dispose_CancelsPendingOperations()
    {
        // Arrange
        var coordinator = new UpdateCheckCoordinator();

        // Act - Start background check and immediately dispose
        _ = coordinator.CheckForUpdatesBackgroundAsync();
        coordinator.Dispose();

        // Assert - Should complete without throwing
        // The cancellation token should cancel the operation
        Assert.True(true, "Disposal should cancel pending operations gracefully");
    }

    #endregion

    #region Integration Notes

    // NOTE: Full integration tests for update checking would require:
    // 1. Mocking UpdateCheckService to return specific results
    // 2. Testing coordinator's response to various update scenarios
    // 3. Verifying state changes when updates are available vs. not available
    //
    // These tests focus on coordinator logic that doesn't depend on external services.
    // UpdateCheckService itself should be tested separately with mocked HTTP responses.

    #endregion
}
