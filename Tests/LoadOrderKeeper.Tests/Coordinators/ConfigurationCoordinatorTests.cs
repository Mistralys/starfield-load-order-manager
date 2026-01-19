using System;
using System.IO;
using LoadOrderKeeper.Coordinators;
using LoadOrderKeeper.Coordinators.Events;
using LoadOrderKeeper.Models;
using Xunit;

namespace LoadOrderKeeper.Tests.Coordinators;

/// <summary>
/// Tests for ConfigurationCoordinator covering configuration validation,
/// state management, event firing, and error detection.
/// </summary>
public sealed class ConfigurationCoordinatorTests : IDisposable
{
    private readonly ConfigurationCoordinator _coordinator;

    public ConfigurationCoordinatorTests()
    {
        _coordinator = new ConfigurationCoordinator();
    }

    public void Dispose()
    {
        _coordinator?.Dispose();
    }

    #region Initialization Tests

    [Fact]
    public void Constructor_InitializesWithInvalidState()
    {
        // Assert
        Assert.False(_coordinator.IsConfigValid);
        Assert.False(_coordinator.ShowErrorBanner);
    }

    [Fact]
    public void Initialize_SetsInvalidState()
    {
        // Arrange
        var coordinator = new ConfigurationCoordinator();

        // Act
        coordinator.Initialize();

        // Assert
        Assert.False(coordinator.IsConfigValid);
        Assert.False(coordinator.ShowErrorBanner);
    }

    #endregion

    #region UpdateConfiguration Tests

    [Fact]
    public void UpdateConfiguration_NullConfig_SetsInvalidState()
    {
        // Act
        _coordinator.UpdateConfiguration(null);

        // Assert
        Assert.False(_coordinator.IsConfigValid);
        Assert.True(_coordinator.ShowErrorBanner);
    }

    [Fact]
    public void UpdateConfiguration_InvalidConfig_SetsInvalidState()
    {
        // Arrange
        var invalidConfig = new AppConfigModel(); // No paths set

        // Act
        _coordinator.UpdateConfiguration(invalidConfig);

        // Assert
        Assert.False(_coordinator.IsConfigValid);
        Assert.True(_coordinator.ShowErrorBanner);
    }

    [Fact]
    public void UpdateConfiguration_ValidConfig_SetsValidState()
    {
        // Arrange
        using var context = new TestConfigContext();

        // Act
        _coordinator.UpdateConfiguration(context.Config);

        // Assert
        Assert.True(_coordinator.IsConfigValid);
        Assert.False(_coordinator.ShowErrorBanner);
    }

    [Fact]
    public void UpdateConfiguration_StateChanges_FiresValidationChangedEvent()
    {
        // Arrange
        using var context = new TestConfigContext();
        
        // Start with null config (invalid)
        _coordinator.UpdateConfiguration(null);
        Assert.False(_coordinator.IsConfigValid);

        ConfigValidationChangedEventArgs? capturedArgs = null;
        _coordinator.ValidationChanged += (sender, args) => capturedArgs = args;

        // Act - Update to valid config
        _coordinator.UpdateConfiguration(context.Config);

        // Assert
        Assert.NotNull(capturedArgs);
        Assert.False(capturedArgs.WasValid);
        Assert.True(capturedArgs.IsValid);
        Assert.True(capturedArgs.StateChanged);
    }

    [Fact]
    public void UpdateConfiguration_NoStateChange_DoesNotFireEvent()
    {
        // Arrange
        var invalidConfig1 = new AppConfigModel();
        var invalidConfig2 = new AppConfigModel();
        
        _coordinator.UpdateConfiguration(invalidConfig1);

        int eventCount = 0;
        _coordinator.ValidationChanged += (sender, args) => eventCount++;

        // Act - Update to another invalid config (state doesn't change)
        _coordinator.UpdateConfiguration(invalidConfig2);

        // Assert
        Assert.Equal(0, eventCount);
    }

    [Fact]
    public void UpdateConfiguration_AfterDisposal_ThrowsObjectDisposedException()
    {
        // Arrange
        _coordinator.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => 
            _coordinator.UpdateConfiguration(new AppConfigModel()));
    }

    #endregion

    #region ValidateConfiguration Tests

    [Fact]
    public void ValidateConfiguration_NullConfig_SetsInvalidState()
    {
        // Arrange
        _coordinator.UpdateConfiguration(null);

        // Act
        _coordinator.ValidateConfiguration();

        // Assert
        Assert.False(_coordinator.IsConfigValid);
        Assert.True(_coordinator.ShowErrorBanner);
    }

    [Fact]
    public void ValidateConfiguration_ValidConfig_SetsValidState()
    {
        // Arrange
        using var context = new TestConfigContext();
        _coordinator.UpdateConfiguration(context.Config);

        // Act
        _coordinator.ValidateConfiguration();

        // Assert
        Assert.True(_coordinator.IsConfigValid);
        Assert.False(_coordinator.ShowErrorBanner);
    }

    [Fact]
    public void ValidateConfiguration_StateChanges_FiresEvent()
    {
        // Arrange
        using var context = new TestConfigContext();
        
        // Start with invalid config
        var invalidConfig = new AppConfigModel();
        _coordinator.UpdateConfiguration(invalidConfig);
        
        // Now set valid config but don't trigger UpdateConfiguration
        var privateField = typeof(ConfigurationCoordinator).GetField("_config", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        privateField?.SetValue(_coordinator, context.Config);

        ConfigValidationChangedEventArgs? capturedArgs = null;
        _coordinator.ValidationChanged += (sender, args) => capturedArgs = args;

        // Act
        _coordinator.ValidateConfiguration();

        // Assert
        Assert.NotNull(capturedArgs);
        Assert.True(capturedArgs.StateChanged);
    }

    [Fact]
    public void ValidateConfiguration_AfterDisposal_ThrowsObjectDisposedException()
    {
        // Arrange
        _coordinator.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => _coordinator.ValidateConfiguration());
    }

    #endregion

    #region GetValidationResult Tests

    [Fact]
    public void GetValidationResult_NullConfig_ReturnsFailedResult()
    {
        // Arrange
        _coordinator.UpdateConfiguration(null);

        // Act
        var result = _coordinator.GetValidationResult();

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("null", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetValidationResult_MissingAppDataPath_ReturnsFailedResult()
    {
        // Arrange
        var config = new AppConfigModel
        {
            StarfieldGamePath = Path.GetTempPath()
        };
        _coordinator.UpdateConfiguration(config);

        // Act
        var result = _coordinator.GetValidationResult();

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("AppData", result.ErrorMessage);
    }

    [Fact]
    public void GetValidationResult_MissingGamePath_ReturnsFailedResult()
    {
        // Arrange
        var config = new AppConfigModel
        {
            StarfieldAppDataPath = Path.GetTempPath()
        };
        _coordinator.UpdateConfiguration(config);

        // Act
        var result = _coordinator.GetValidationResult();

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Game path", result.ErrorMessage);
    }

    [Fact]
    public void GetValidationResult_NonexistentAppDataPath_ReturnsFailedResult()
    {
        // Arrange
        var config = new AppConfigModel
        {
            StarfieldAppDataPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()),
            StarfieldGamePath = Path.GetTempPath()
        };
        _coordinator.UpdateConfiguration(config);

        // Act
        var result = _coordinator.GetValidationResult();

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("does not exist", result.ErrorMessage);
    }

    [Fact]
    public void GetValidationResult_NonexistentGamePath_ReturnsFailedResult()
    {
        // Arrange
        var config = new AppConfigModel
        {
            StarfieldAppDataPath = Path.GetTempPath(),
            StarfieldGamePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())
        };
        _coordinator.UpdateConfiguration(config);

        // Act
        var result = _coordinator.GetValidationResult();

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("does not exist", result.ErrorMessage);
    }

    [Fact]
    public void GetValidationResult_MissingDataFolder_ReturnsFailedResult()
    {
        // Arrange
        using var context = new TestConfigContext();
        
        // Delete the Data folder
        var dataPath = Path.Combine(context.StarfieldGamePath, "Data");
        if (Directory.Exists(dataPath))
        {
            Directory.Delete(dataPath, true);
        }

        _coordinator.UpdateConfiguration(context.Config);

        // Act
        var result = _coordinator.GetValidationResult();

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Data folder", result.ErrorMessage);
    }

    [Fact]
    public void GetValidationResult_MissingPluginsFile_ReturnsFailedResult()
    {
        // Arrange
        using var context = new TestConfigContext();
        
        // Delete Plugins.txt
        if (File.Exists(context.PluginsFilePath))
        {
            File.Delete(context.PluginsFilePath);
        }

        _coordinator.UpdateConfiguration(context.Config);

        // Act
        var result = _coordinator.GetValidationResult();

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Plugins.txt", result.ErrorMessage);
    }

    [Fact]
    public void GetValidationResult_ValidConfig_ReturnsSuccessResult()
    {
        // Arrange
        using var context = new TestConfigContext();
        _coordinator.UpdateConfiguration(context.Config);

        // Act
        var result = _coordinator.GetValidationResult();

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void GetValidationResult_ProfilesFolderCreatedIfMissing_ReturnsSuccess()
    {
        // Arrange
        using var context = new TestConfigContext();
        
        // Ensure Profiles folder doesn't exist
        var profilesFolder = Path.Combine(context.Config.StarfieldAppDataPath, "Profiles");
        if (Directory.Exists(profilesFolder))
        {
            Directory.Delete(profilesFolder, true);
        }

        _coordinator.UpdateConfiguration(context.Config);

        // Act
        var result = _coordinator.GetValidationResult();

        // Assert
        Assert.True(result.IsValid);
        Assert.True(Directory.Exists(profilesFolder)); // Should be created
    }

    [Fact]
    public void GetValidationResult_AfterDisposal_ThrowsObjectDisposedException()
    {
        // Arrange
        _coordinator.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => _coordinator.GetValidationResult());
    }

    #endregion

    #region ValidationResult Tests

    [Fact]
    public void ValidationResult_Success_CreatesValidResult()
    {
        // Act
        var result = ValidationResult.Success();

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void ValidationResult_Failed_CreatesInvalidResultWithMessage()
    {
        // Act
        var result = ValidationResult.Failed("Test error message");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Test error message", result.ErrorMessage);
    }

    #endregion

    #region Event Tests

    [Fact]
    public void ValidationChanged_MultipleSubscribers_AllReceiveEvent()
    {
        // Arrange
        using var context = new TestConfigContext();
        
        int subscriber1CallCount = 0;
        int subscriber2CallCount = 0;
        
        _coordinator.ValidationChanged += (sender, args) => subscriber1CallCount++;
        _coordinator.ValidationChanged += (sender, args) => subscriber2CallCount++;

        // Act - Update from invalid (false) to valid (true)
        // Note: ConfigurationCoordinator fires the event in both UpdateConfiguration 
        // and ValidateConfiguration, so we expect 2 events for a state change
        _coordinator.UpdateConfiguration(context.Config);

        // Assert - Both subscribers should receive the same number of events
        Assert.Equal(subscriber1CallCount, subscriber2CallCount);
        Assert.True(subscriber1CallCount >= 1, "At least one event should fire");
    }

    [Fact]
    public void ValidationChangedEventArgs_StateChanged_ReturnsTrue()
    {
        // Arrange
        using var context = new TestConfigContext();
        
        ConfigValidationChangedEventArgs? capturedArgs = null;
        _coordinator.ValidationChanged += (sender, args) => capturedArgs = args;

        // Act
        _coordinator.UpdateConfiguration(context.Config);

        // Assert
        Assert.NotNull(capturedArgs);
        Assert.True(capturedArgs.StateChanged);
    }

    [Fact]
    public void ValidationChangedEventArgs_ValidToInvalid_CapturesTotransition()
    {
        // Arrange
        using var context = new TestConfigContext();
        _coordinator.UpdateConfiguration(context.Config);
        Assert.True(_coordinator.IsConfigValid);

        ConfigValidationChangedEventArgs? capturedArgs = null;
        _coordinator.ValidationChanged += (sender, args) => capturedArgs = args;

        // Act - Update to invalid config
        _coordinator.UpdateConfiguration(null);

        // Assert
        Assert.NotNull(capturedArgs);
        Assert.True(capturedArgs.WasValid);
        Assert.False(capturedArgs.IsValid);
        Assert.True(capturedArgs.StateChanged);
    }

    #endregion

    #region Error Banner Tests

    [Fact]
    public void ShowErrorBanner_InvalidConfig_ReturnsTrue()
    {
        // Arrange
        var invalidConfig = new AppConfigModel();

        // Act
        _coordinator.UpdateConfiguration(invalidConfig);

        // Assert
        Assert.True(_coordinator.ShowErrorBanner);
    }

    [Fact]
    public void ShowErrorBanner_ValidConfig_ReturnsFalse()
    {
        // Arrange
        using var context = new TestConfigContext();

        // Act
        _coordinator.UpdateConfiguration(context.Config);

        // Assert
        Assert.False(_coordinator.ShowErrorBanner);
    }

    [Fact]
    public void ShowErrorBanner_TogglesWithValidationState()
    {
        // Arrange
        using var context = new TestConfigContext();
        var invalidConfig = new AppConfigModel();

        // Act & Assert - Start invalid
        _coordinator.UpdateConfiguration(invalidConfig);
        Assert.True(_coordinator.ShowErrorBanner);

        // Act & Assert - Switch to valid
        _coordinator.UpdateConfiguration(context.Config);
        Assert.False(_coordinator.ShowErrorBanner);

        // Act & Assert - Switch back to invalid
        _coordinator.UpdateConfiguration(null);
        Assert.True(_coordinator.ShowErrorBanner);
    }

    #endregion

    #region Disposal Tests

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var coordinator = new ConfigurationCoordinator();

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
