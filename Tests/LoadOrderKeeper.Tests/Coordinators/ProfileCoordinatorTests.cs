using System;
using System.Threading.Tasks;
using LoadOrderKeeper.Coordinators;
using LoadOrderKeeper.Coordinators.Events;
using LoadOrderKeeper.Models;
using LoadOrderKeeper.Services;
using Xunit;

namespace LoadOrderKeeper.Tests.Coordinators;

/// <summary>
/// Tests for ProfileCoordinator covering configuration updates, profile switching,
/// event firing, and state management.
/// </summary>
public sealed class ProfileCoordinatorTests : IDisposable
{
    private readonly ProfileCoordinator _coordinator;

    public ProfileCoordinatorTests()
    {
        _coordinator = new ProfileCoordinator();
    }

    public void Dispose()
    {
        _coordinator?.Dispose();
    }

    #region Initialization Tests

    [Fact]
    public void Constructor_InitializesWithDefaultProfile()
    {
        // Assert
        Assert.NotNull(_coordinator.ActiveProfile);
        Assert.Equal("default", _coordinator.ActiveProfile.Id);
        Assert.Equal("Default", _coordinator.ActiveProfileLabel);
        Assert.True(_coordinator.ActiveProfile.IsDefault);
    }

    [Fact]
    public void Initialize_SetsDefaultProfile()
    {
        // Arrange
        var coordinator = new ProfileCoordinator();

        // Act
        coordinator.Initialize();

        // Assert
        Assert.Equal("default", coordinator.ActiveProfile.Id);
        Assert.Equal("Default", coordinator.ActiveProfileLabel);
    }

    #endregion

    #region UpdateConfiguration Tests

    [Fact]
    public void UpdateConfiguration_NullConfig_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _coordinator.UpdateConfiguration(null!));
    }

    [Fact]
    public void UpdateConfiguration_ValidConfig_AcceptsConfiguration()
    {
        // Arrange
        using var context = new TestConfigContext();

        // Act & Assert - Should not throw
        _coordinator.UpdateConfiguration(context.Config);
    }

    [Fact]
    public void UpdateConfiguration_AfterDisposal_ThrowsObjectDisposedException()
    {
        // Arrange
        using var context = new TestConfigContext();
        _coordinator.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => _coordinator.UpdateConfiguration(context.Config));
    }

    #endregion

    #region RefreshActiveProfileAsync Tests

    [Fact]
    public async Task RefreshActiveProfileAsync_NoConfig_SetsDefaultProfile()
    {
        // Act
        await _coordinator.RefreshActiveProfileAsync();

        // Assert
        Assert.Equal("default", _coordinator.ActiveProfile.Id);
        Assert.Equal("Default", _coordinator.ActiveProfileLabel);
    }

    [Fact]
    public async Task RefreshActiveProfileAsync_InvalidConfig_SetsDefaultProfile()
    {
        // Arrange
        var invalidConfig = new AppConfigModel(); // No paths set
        _coordinator.UpdateConfiguration(invalidConfig);

        // Act
        await _coordinator.RefreshActiveProfileAsync();

        // Assert
        Assert.Equal("default", _coordinator.ActiveProfile.Id);
        Assert.Equal("Default", _coordinator.ActiveProfileLabel);
    }

    [Fact]
    public async Task RefreshActiveProfileAsync_ValidConfig_LoadsActiveProfile()
    {
        // Arrange
        using var context = new TestConfigContext();
        await context.WritePluginsAsync("*mod1.esm");
        _coordinator.UpdateConfiguration(context.Config);

        // Act
        await _coordinator.RefreshActiveProfileAsync();

        // Assert
        Assert.NotNull(_coordinator.ActiveProfile);
        Assert.Equal("default", _coordinator.ActiveProfile.Id);
    }

    [Fact]
    public async Task RefreshActiveProfileAsync_ProfileIdChanged_FiresProfileChangedEvent()
    {
        // Arrange
        using var context = new TestConfigContext();
        await context.WritePluginsAsync("*mod1.esm");
        
        // Create a custom profile and switch to it
        var customProfile = await ProfileService.CreateProfileAsync(context.Config, "Test Profile", "");
        context.Config.ActiveProfileId = customProfile.Id;
        await SettingsService.SaveSettingsAsync(context.Config);
        
        _coordinator.UpdateConfiguration(context.Config);

        ProfileChangedEventArgs? capturedArgs = null;
        _coordinator.ProfileChanged += (sender, args) => capturedArgs = args;

        // Act
        await _coordinator.RefreshActiveProfileAsync();

        // Assert
        Assert.NotNull(capturedArgs);
        Assert.Equal("default", capturedArgs.OldProfile.Id);
        Assert.Equal(customProfile.Id, capturedArgs.NewProfile.Id);
    }

    [Fact]
    public async Task RefreshActiveProfileAsync_SameProfile_DoesNotFireEvent()
    {
        // Arrange
        using var context = new TestConfigContext();
        await context.WritePluginsAsync("*mod1.esm");
        _coordinator.UpdateConfiguration(context.Config);

        // Refresh once to establish baseline
        await _coordinator.RefreshActiveProfileAsync();

        int eventCount = 0;
        _coordinator.ProfileChanged += (sender, args) => eventCount++;

        // Act - Refresh again with same profile
        await _coordinator.RefreshActiveProfileAsync();

        // Assert - Event should not fire since profile didn't change
        Assert.Equal(0, eventCount);
    }

    [Fact]
    public async Task RefreshActiveProfileAsync_ServiceThrowsException_FallsBackToDefault()
    {
        // Arrange
        using var context = new TestConfigContext();
        // Set up an invalid profile ID that will cause GetActiveProfileAsync to fail gracefully
        context.Config.ActiveProfileId = "nonexistent-profile";
        _coordinator.UpdateConfiguration(context.Config);

        // Act
        await _coordinator.RefreshActiveProfileAsync();

        // Assert - Should fall back to default profile
        Assert.Equal("default", _coordinator.ActiveProfile.Id);
    }

    #endregion

    #region SwitchProfileAsync Tests

    [Fact]
    public async Task SwitchProfileAsync_NoConfig_ReturnsFalse()
    {
        // Act
        bool result = await _coordinator.SwitchProfileAsync("test-profile");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SwitchProfileAsync_InvalidConfig_ReturnsFalse()
    {
        // Arrange
        var invalidConfig = new AppConfigModel();
        _coordinator.UpdateConfiguration(invalidConfig);

        // Act
        bool result = await _coordinator.SwitchProfileAsync("test-profile");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SwitchProfileAsync_NullProfileId_ThrowsArgumentException()
    {
        // Arrange
        using var context = new TestConfigContext();
        _coordinator.UpdateConfiguration(context.Config);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _coordinator.SwitchProfileAsync(null!));
    }

    [Fact]
    public async Task SwitchProfileAsync_EmptyProfileId_ThrowsArgumentException()
    {
        // Arrange
        using var context = new TestConfigContext();
        _coordinator.UpdateConfiguration(context.Config);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _coordinator.SwitchProfileAsync(string.Empty));
    }

    [Fact]
    public async Task SwitchProfileAsync_ValidProfile_ReturnsTrue()
    {
        // Arrange
        using var context = new TestConfigContext();
        await context.WritePluginsAsync("*mod1.esm");
        
        var newProfile = await ProfileService.CreateProfileAsync(context.Config, "New Profile", "Test");
        _coordinator.UpdateConfiguration(context.Config);

        // Act
        bool result = await _coordinator.SwitchProfileAsync(newProfile.Id);

        // Assert
        Assert.True(result);
        Assert.Equal(newProfile.Id, _coordinator.ActiveProfile.Id);
    }

    [Fact]
    public async Task SwitchProfileAsync_NonexistentProfile_ReturnsFalse()
    {
        // Arrange
        using var context = new TestConfigContext();
        await context.WritePluginsAsync("*mod1.esm");
        _coordinator.UpdateConfiguration(context.Config);

        // Act
        bool result = await _coordinator.SwitchProfileAsync("nonexistent-profile");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SwitchProfileAsync_Success_UpdatesActiveProfile()
    {
        // Arrange
        using var context = new TestConfigContext();
        await context.WritePluginsAsync("*mod1.esm");
        
        var newProfile = await ProfileService.CreateProfileAsync(context.Config, "Test Profile", "Description");
        _coordinator.UpdateConfiguration(context.Config);

        // Act
        await _coordinator.SwitchProfileAsync(newProfile.Id);

        // Assert
        Assert.Equal(newProfile.Id, _coordinator.ActiveProfile.Id);
        Assert.Equal("Test Profile", _coordinator.ActiveProfileLabel);
    }

    [Fact]
    public async Task SwitchProfileAsync_Success_RefreshesActiveProfile()
    {
        // Arrange
        using var context = new TestConfigContext();
        await context.WritePluginsAsync("*mod1.esm");
        
        var profile1 = await ProfileService.CreateProfileAsync(context.Config, "Profile 1", "");
        var profile2 = await ProfileService.CreateProfileAsync(context.Config, "Profile 2", "");
        _coordinator.UpdateConfiguration(context.Config);

        // Act - Switch to profile 1
        await _coordinator.SwitchProfileAsync(profile1.Id);
        Assert.Equal(profile1.Id, _coordinator.ActiveProfile.Id);

        // Act - Switch to profile 2
        await _coordinator.SwitchProfileAsync(profile2.Id);

        // Assert
        Assert.Equal(profile2.Id, _coordinator.ActiveProfile.Id);
        Assert.Equal("Profile 2", _coordinator.ActiveProfileLabel);
    }

    #endregion

    #region IsActiveProfile Tests

    [Fact]
    public void IsActiveProfile_DefaultProfile_ReturnsTrue()
    {
        // Act & Assert
        Assert.True(_coordinator.IsActiveProfile("default"));
    }

    [Fact]
    public void IsActiveProfile_DefaultProfile_CaseInsensitive()
    {
        // Act & Assert
        Assert.True(_coordinator.IsActiveProfile("DEFAULT"));
        Assert.True(_coordinator.IsActiveProfile("Default"));
        Assert.True(_coordinator.IsActiveProfile("dEfAuLt"));
    }

    [Fact]
    public void IsActiveProfile_NonActiveProfile_ReturnsFalse()
    {
        // Act & Assert
        Assert.False(_coordinator.IsActiveProfile("other-profile"));
    }

    [Fact]
    public async Task IsActiveProfile_AfterProfileSwitch_ReturnsCorrectValue()
    {
        // Arrange
        using var context = new TestConfigContext();
        await context.WritePluginsAsync("*mod1.esm");
        
        var newProfile = await ProfileService.CreateProfileAsync(context.Config, "Test Profile", "");
        _coordinator.UpdateConfiguration(context.Config);
        await _coordinator.SwitchProfileAsync(newProfile.Id);

        // Act & Assert
        Assert.True(_coordinator.IsActiveProfile(newProfile.Id));
        Assert.False(_coordinator.IsActiveProfile("default"));
    }

    [Fact]
    public void IsActiveProfile_AfterDisposal_ThrowsObjectDisposedException()
    {
        // Arrange
        _coordinator.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => _coordinator.IsActiveProfile("default"));
    }

    #endregion

    #region ProfileChanged Event Tests

    [Fact]
    public async Task ProfileChanged_WhenProfileSwitches_FiresEvent()
    {
        // Arrange
        using var context = new TestConfigContext();
        await context.WritePluginsAsync("*mod1.esm");
        
        var newProfile = await ProfileService.CreateProfileAsync(context.Config, "New Profile", "");
        _coordinator.UpdateConfiguration(context.Config);

        ProfileChangedEventArgs? capturedArgs = null;
        _coordinator.ProfileChanged += (sender, args) => capturedArgs = args;

        // Act
        await _coordinator.SwitchProfileAsync(newProfile.Id);

        // Assert
        Assert.NotNull(capturedArgs);
        Assert.Equal("default", capturedArgs.OldProfile.Id);
        Assert.Equal(newProfile.Id, capturedArgs.NewProfile.Id);
    }

    [Fact]
    public async Task ProfileChanged_MultipleSubscribers_AllReceiveEvent()
    {
        // Arrange
        using var context = new TestConfigContext();
        await context.WritePluginsAsync("*mod1.esm");
        
        var newProfile = await ProfileService.CreateProfileAsync(context.Config, "New Profile", "");
        _coordinator.UpdateConfiguration(context.Config);

        int subscriber1CallCount = 0;
        int subscriber2CallCount = 0;
        
        _coordinator.ProfileChanged += (sender, args) => subscriber1CallCount++;
        _coordinator.ProfileChanged += (sender, args) => subscriber2CallCount++;

        // Act
        await _coordinator.SwitchProfileAsync(newProfile.Id);

        // Assert
        Assert.Equal(1, subscriber1CallCount);
        Assert.Equal(1, subscriber2CallCount);
    }

    [Fact]
    public async Task ProfileChanged_SwitchToSameProfile_DoesNotFireEvent()
    {
        // Arrange
        using var context = new TestConfigContext();
        await context.WritePluginsAsync("*mod1.esm");
        _coordinator.UpdateConfiguration(context.Config);

        // Refresh to establish baseline (fires one event due to initial load)
        await _coordinator.RefreshActiveProfileAsync();

        int eventCount = 0;
        _coordinator.ProfileChanged += (sender, args) => eventCount++;

        // Act - Switch to default again (already active)
        await _coordinator.SwitchProfileAsync("default");

        // Assert - Should fire during refresh if ProfileService returns default
        // But since the profile ID doesn't change, event shouldn't fire
        Assert.Equal(0, eventCount);
    }

    #endregion

    #region Default Profile Fallback Tests

    [Fact]
    public async Task SetDefaultProfile_WhenConfigInvalid_SetsDefault()
    {
        // Arrange
        using var context = new TestConfigContext();
        await context.WritePluginsAsync("*mod1.esm");
        
        // Create and switch to custom profile
        var customProfile = await ProfileService.CreateProfileAsync(context.Config, "Custom", "");
        _coordinator.UpdateConfiguration(context.Config);
        await _coordinator.SwitchProfileAsync(customProfile.Id);
        
        Assert.Equal(customProfile.Id, _coordinator.ActiveProfile.Id);

        // Now invalidate the config
        var invalidConfig = new AppConfigModel();
        _coordinator.UpdateConfiguration(invalidConfig);

        // Act - Refresh with invalid config should fall back to default
        await _coordinator.RefreshActiveProfileAsync();

        // Assert
        Assert.Equal("default", _coordinator.ActiveProfile.Id);
        Assert.Equal("Default", _coordinator.ActiveProfileLabel);
    }

    [Fact]
    public async Task SetDefaultProfile_FiresEventWhenChanging()
    {
        // Arrange
        using var context = new TestConfigContext();
        await context.WritePluginsAsync("*mod1.esm");
        
        var customProfile = await ProfileService.CreateProfileAsync(context.Config, "Custom", "");
        context.Config.ActiveProfileId = customProfile.Id;
        await SettingsService.SaveSettingsAsync(context.Config);
        _coordinator.UpdateConfiguration(context.Config);
        
        // Load the custom profile
        await _coordinator.RefreshActiveProfileAsync();
        Assert.Equal(customProfile.Id, _coordinator.ActiveProfile.Id);

        // Now set up invalid config
        var invalidConfig = new AppConfigModel();
        _coordinator.UpdateConfiguration(invalidConfig);

        ProfileChangedEventArgs? capturedArgs = null;
        _coordinator.ProfileChanged += (sender, args) => capturedArgs = args;

        // Act - Refresh with invalid config
        await _coordinator.RefreshActiveProfileAsync();

        // Assert
        Assert.NotNull(capturedArgs);
        Assert.Equal(customProfile.Id, capturedArgs.OldProfile.Id);
        Assert.Equal("default", capturedArgs.NewProfile.Id);
    }

    #endregion

    #region Disposal Tests

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var coordinator = new ProfileCoordinator();

        // Act & Assert - Should not throw
        coordinator.Dispose();
        coordinator.Dispose();
    }

    [Fact]
    public void Dispose_ClearsConfiguration()
    {
        // Arrange
        using var context = new TestConfigContext();
        _coordinator.UpdateConfiguration(context.Config);

        // Act
        _coordinator.Dispose();

        // Assert - Further operations should throw
        Assert.Throws<ObjectDisposedException>(() => 
            _coordinator.UpdateConfiguration(context.Config));
    }

    #endregion
}
