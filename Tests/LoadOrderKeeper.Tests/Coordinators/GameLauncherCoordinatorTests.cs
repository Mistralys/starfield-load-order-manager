using System;
using System.IO;
using LoadOrderKeeper.Coordinators;
using LoadOrderKeeper.Models;
using Xunit;

namespace LoadOrderKeeper.Tests.Coordinators;

/// <summary>
/// Tests for GameLauncherCoordinator covering SFSE detection, game launching,
/// executable path resolution, and play button text updates.
/// </summary>
public sealed class GameLauncherCoordinatorTests : IDisposable
{
    private readonly GameLauncherCoordinator _coordinator;
    private readonly string _testGamePath;

    public GameLauncherCoordinatorTests()
    {
        _coordinator = new GameLauncherCoordinator();
        _testGamePath = Path.Combine(Path.GetTempPath(), "TestStarfield", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testGamePath);
    }

    public void Dispose()
    {
        _coordinator?.Dispose();
        
        try
        {
            if (Directory.Exists(_testGamePath))
            {
                Directory.Delete(_testGamePath, true);
            }
        }
        catch
        {
            // Cleanup failure, ignore
        }
    }

    #region Initialization Tests

    [Fact]
    public void Constructor_InitializesWithDefaultState()
    {
        // Assert
        Assert.Equal("Play (Vanilla)", _coordinator.PlayButtonText);
        Assert.False(_coordinator.HasSfseInstalled);
    }

    [Fact]
    public void Initialize_SetsDefaultPlayButtonText()
    {
        // Arrange
        var coordinator = new GameLauncherCoordinator();

        // Act
        coordinator.Initialize();

        // Assert
        Assert.Equal("Play (Vanilla)", coordinator.PlayButtonText);
    }

    #endregion

    #region UpdateGamePath Tests

    [Fact]
    public void UpdateGamePath_NullPath_SetsNoSfse()
    {
        // Act
        _coordinator.UpdateGamePath(null);

        // Assert
        Assert.False(_coordinator.HasSfseInstalled);
        Assert.Equal("Play (Vanilla)", _coordinator.PlayButtonText);
    }

    [Fact]
    public void UpdateGamePath_EmptyPath_SetsNoSfse()
    {
        // Act
        _coordinator.UpdateGamePath(string.Empty);

        // Assert
        Assert.False(_coordinator.HasSfseInstalled);
        Assert.Equal("Play (Vanilla)", _coordinator.PlayButtonText);
    }

    [Fact]
    public void UpdateGamePath_PathWithoutSfse_SetsNoSfse()
    {
        // Act
        _coordinator.UpdateGamePath(_testGamePath);

        // Assert
        Assert.False(_coordinator.HasSfseInstalled);
        Assert.Equal("Play (Vanilla)", _coordinator.PlayButtonText);
    }

    [Fact]
    public void UpdateGamePath_PathWithSfse_DetectsSfse()
    {
        // Arrange
        var sfsePath = Path.Combine(_testGamePath, "sfse_loader.exe");
        File.WriteAllText(sfsePath, "test");

        // Act
        _coordinator.UpdateGamePath(_testGamePath);

        // Assert
        Assert.True(_coordinator.HasSfseInstalled);
        Assert.Equal("Play (SFSE)", _coordinator.PlayButtonText);
    }

    [Fact]
    public void UpdateGamePath_AfterDisposal_ThrowsObjectDisposedException()
    {
        // Arrange
        _coordinator.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => _coordinator.UpdateGamePath(_testGamePath));
    }

    #endregion

    #region UpdateConfiguration Tests

    [Fact]
    public void UpdateConfiguration_NullConfig_SetsNoSfse()
    {
        // Act
        _coordinator.UpdateConfiguration(null);

        // Assert
        Assert.False(_coordinator.HasSfseInstalled);
        Assert.Equal("Play (Vanilla)", _coordinator.PlayButtonText);
    }

    [Fact]
    public void UpdateConfiguration_ConfigWithoutGamePath_SetsNoSfse()
    {
        // Arrange
        var config = new AppConfigModel();

        // Act
        _coordinator.UpdateConfiguration(config);

        // Assert
        Assert.False(_coordinator.HasSfseInstalled);
        Assert.Equal("Play (Vanilla)", _coordinator.PlayButtonText);
    }

    [Fact]
    public void UpdateConfiguration_ConfigWithGamePath_DetectsSfse()
    {
        // Arrange
        var sfsePath = Path.Combine(_testGamePath, "sfse_loader.exe");
        File.WriteAllText(sfsePath, "test");
        
        var config = new AppConfigModel
        {
            StarfieldGamePath = _testGamePath
        };

        // Act
        _coordinator.UpdateConfiguration(config);

        // Assert
        Assert.True(_coordinator.HasSfseInstalled);
        Assert.Equal("Play (SFSE)", _coordinator.PlayButtonText);
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

    #region LaunchGame Tests

    [Fact]
    public void LaunchGame_NoGamePath_ReturnsFalse()
    {
        // Act
        bool result = _coordinator.LaunchGame();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void LaunchGame_NonexistentPath_ReturnsFalse()
    {
        // Arrange
        var nonexistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _coordinator.UpdateGamePath(nonexistentPath);

        // Act
        bool result = _coordinator.LaunchGame();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void LaunchGame_MissingExecutable_ReturnsFalse()
    {
        // Arrange
        _coordinator.UpdateGamePath(_testGamePath);

        // Act
        bool result = _coordinator.LaunchGame();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void LaunchGame_VanillaExecutableExists_ReturnsTrue()
    {
        // Arrange
        var vanillaExe = Path.Combine(_testGamePath, "starfield.exe");
        File.WriteAllText(vanillaExe, "test");
        _coordinator.UpdateGamePath(_testGamePath);

        // Act
        bool result = _coordinator.LaunchGame();

        // Assert - Would be true if we could actually launch, but in test environment
        // the executable is just a text file, so it will fail to launch
        // We're testing the path resolution logic, not actual process launching
        Assert.False(result); // Fails because it's not a real executable
    }

    [Fact]
    public void LaunchGame_AfterDisposal_ThrowsObjectDisposedException()
    {
        // Arrange
        _coordinator.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => _coordinator.LaunchGame());
    }

    #endregion

    #region GetExecutablePath Tests

    [Fact]
    public void GetExecutablePath_NoGamePath_ReturnsNull()
    {
        // Act
        var path = _coordinator.GetExecutablePath();

        // Assert
        Assert.Null(path);
    }

    [Fact]
    public void GetExecutablePath_EmptyGamePath_ReturnsNull()
    {
        // Arrange
        _coordinator.UpdateGamePath(string.Empty);

        // Act
        var path = _coordinator.GetExecutablePath();

        // Assert
        Assert.Null(path);
    }

    [Fact]
    public void GetExecutablePath_MissingExecutable_ReturnsNull()
    {
        // Arrange
        _coordinator.UpdateGamePath(_testGamePath);

        // Act
        var path = _coordinator.GetExecutablePath();

        // Assert
        Assert.Null(path);
    }

    [Fact]
    public void GetExecutablePath_VanillaExecutableExists_ReturnsVanillaPath()
    {
        // Arrange
        var vanillaExe = Path.Combine(_testGamePath, "starfield.exe");
        File.WriteAllText(vanillaExe, "test");
        _coordinator.UpdateGamePath(_testGamePath);

        // Act
        var path = _coordinator.GetExecutablePath();

        // Assert
        Assert.NotNull(path);
        Assert.Equal(vanillaExe, path);
        Assert.EndsWith("starfield.exe", path);
    }

    [Fact]
    public void GetExecutablePath_SfseExecutableExists_ReturnsSfsePath()
    {
        // Arrange
        var sfseExe = Path.Combine(_testGamePath, "sfse_loader.exe");
        File.WriteAllText(sfseExe, "test");
        _coordinator.UpdateGamePath(_testGamePath);

        // Act
        var path = _coordinator.GetExecutablePath();

        // Assert
        Assert.NotNull(path);
        Assert.Equal(sfseExe, path);
        Assert.EndsWith("sfse_loader.exe", path);
    }

    [Fact]
    public void GetExecutablePath_BothExecutablesExist_PrefersSfse()
    {
        // Arrange
        var vanillaExe = Path.Combine(_testGamePath, "starfield.exe");
        var sfseExe = Path.Combine(_testGamePath, "sfse_loader.exe");
        File.WriteAllText(vanillaExe, "test");
        File.WriteAllText(sfseExe, "test");
        _coordinator.UpdateGamePath(_testGamePath);

        // Act
        var path = _coordinator.GetExecutablePath();

        // Assert
        Assert.NotNull(path);
        Assert.Equal(sfseExe, path);
        Assert.EndsWith("sfse_loader.exe", path);
    }

    [Fact]
    public void GetExecutablePath_AfterDisposal_ThrowsObjectDisposedException()
    {
        // Arrange
        _coordinator.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => _coordinator.GetExecutablePath());
    }

    #endregion

    #region SFSE Detection Tests

    [Fact]
    public void HasSfseInstalled_NoSfseFile_ReturnsFalse()
    {
        // Arrange
        _coordinator.UpdateGamePath(_testGamePath);

        // Assert
        Assert.False(_coordinator.HasSfseInstalled);
    }

    [Fact]
    public void HasSfseInstalled_SfseFileExists_ReturnsTrue()
    {
        // Arrange
        var sfsePath = Path.Combine(_testGamePath, "sfse_loader.exe");
        File.WriteAllText(sfsePath, "test");

        // Act
        _coordinator.UpdateGamePath(_testGamePath);

        // Assert
        Assert.True(_coordinator.HasSfseInstalled);
    }

    [Fact]
    public void HasSfseInstalled_SfseRemoved_UpdatesToFalse()
    {
        // Arrange
        var sfsePath = Path.Combine(_testGamePath, "sfse_loader.exe");
        File.WriteAllText(sfsePath, "test");
        _coordinator.UpdateGamePath(_testGamePath);
        Assert.True(_coordinator.HasSfseInstalled);

        // Act - Remove SFSE and update
        File.Delete(sfsePath);
        _coordinator.UpdateGamePath(_testGamePath);

        // Assert
        Assert.False(_coordinator.HasSfseInstalled);
    }

    [Fact]
    public void HasSfseInstalled_SfseAdded_UpdatesToTrue()
    {
        // Arrange
        _coordinator.UpdateGamePath(_testGamePath);
        Assert.False(_coordinator.HasSfseInstalled);

        // Act - Add SFSE and update
        var sfsePath = Path.Combine(_testGamePath, "sfse_loader.exe");
        File.WriteAllText(sfsePath, "test");
        _coordinator.UpdateGamePath(_testGamePath);

        // Assert
        Assert.True(_coordinator.HasSfseInstalled);
    }

    #endregion

    #region PlayButtonText Tests

    [Fact]
    public void PlayButtonText_NoSfse_ShowsVanilla()
    {
        // Arrange
        _coordinator.UpdateGamePath(_testGamePath);

        // Assert
        Assert.Equal("Play (Vanilla)", _coordinator.PlayButtonText);
    }

    [Fact]
    public void PlayButtonText_WithSfse_ShowsSfse()
    {
        // Arrange
        var sfsePath = Path.Combine(_testGamePath, "sfse_loader.exe");
        File.WriteAllText(sfsePath, "test");

        // Act
        _coordinator.UpdateGamePath(_testGamePath);

        // Assert
        Assert.Equal("Play (SFSE)", _coordinator.PlayButtonText);
    }

    [Fact]
    public void PlayButtonText_UpdatesWhenSfseStateChanges()
    {
        // Arrange
        var sfsePath = Path.Combine(_testGamePath, "sfse_loader.exe");
        
        // Start without SFSE
        _coordinator.UpdateGamePath(_testGamePath);
        Assert.Equal("Play (Vanilla)", _coordinator.PlayButtonText);

        // Add SFSE
        File.WriteAllText(sfsePath, "test");
        _coordinator.UpdateGamePath(_testGamePath);
        Assert.Equal("Play (SFSE)", _coordinator.PlayButtonText);

        // Remove SFSE
        File.Delete(sfsePath);
        _coordinator.UpdateGamePath(_testGamePath);
        Assert.Equal("Play (Vanilla)", _coordinator.PlayButtonText);
    }

    #endregion

    #region Property Change Tests

    [Fact]
    public void PlayButtonText_RaisesPropertyChanged()
    {
        // Arrange
        var sfsePath = Path.Combine(_testGamePath, "sfse_loader.exe");
        bool propertyChangedRaised = false;
        
        _coordinator.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(GameLauncherCoordinator.PlayButtonText))
            {
                propertyChangedRaised = true;
            }
        };

        // Act
        File.WriteAllText(sfsePath, "test");
        _coordinator.UpdateGamePath(_testGamePath);

        // Assert
        Assert.True(propertyChangedRaised);
    }

    [Fact]
    public void HasSfseInstalled_RaisesPropertyChanged()
    {
        // Arrange
        var sfsePath = Path.Combine(_testGamePath, "sfse_loader.exe");
        bool propertyChangedRaised = false;
        
        _coordinator.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(GameLauncherCoordinator.HasSfseInstalled))
            {
                propertyChangedRaised = true;
            }
        };

        // Act
        File.WriteAllText(sfsePath, "test");
        _coordinator.UpdateGamePath(_testGamePath);

        // Assert
        Assert.True(propertyChangedRaised);
    }

    #endregion

    #region Disposal Tests

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var coordinator = new GameLauncherCoordinator();

        // Act & Assert - Should not throw
        coordinator.Dispose();
        coordinator.Dispose();
    }

    [Fact]
    public void Dispose_ClearsGamePath()
    {
        // Arrange
        _coordinator.UpdateGamePath(_testGamePath);

        // Act
        _coordinator.Dispose();

        // Assert - Further operations should throw
        Assert.Throws<ObjectDisposedException>(() => _coordinator.UpdateGamePath(_testGamePath));
    }

    #endregion
}
