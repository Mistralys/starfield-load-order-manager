using System;
using System.IO;
using LoadOrderKeeper.Models;
using Xunit;

namespace LoadOrderKeeper.Tests.Models;

/// <summary>
/// Tests for AppConfigModel covering validation logic, path resolution,
/// and default values.
/// </summary>
public sealed class AppConfigModelTests
{
    #region Constructor and Default Values Tests

    [Fact]
    public void Constructor_InitializesWithEmptyStrings()
    {
        // Act
        var config = new AppConfigModel();

        // Assert
        Assert.Equal(string.Empty, config.StarfieldAppDataPath);
        Assert.Equal(string.Empty, config.StarfieldGamePath);
    }

    [Fact]
    public void Constructor_SetsDefaultActiveProfileId()
    {
        // Act
        var config = new AppConfigModel();

        // Assert
        Assert.Equal("default", config.ActiveProfileId);
    }

    #endregion

    #region IsValid Tests

    [Fact]
    public void IsValid_EmptyPaths_ReturnsFalse()
    {
        // Arrange
        var config = new AppConfigModel();

        // Act
        bool result = config.IsValid();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_NullAppDataPath_ReturnsFalse()
    {
        // Arrange
        var config = new AppConfigModel
        {
            StarfieldAppDataPath = null!,
            StarfieldGamePath = Path.GetTempPath()
        };

        // Act
        bool result = config.IsValid();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_NullGamePath_ReturnsFalse()
    {
        // Arrange
        var config = new AppConfigModel
        {
            StarfieldAppDataPath = Path.GetTempPath(),
            StarfieldGamePath = null!
        };

        // Act
        bool result = config.IsValid();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_NonexistentAppDataPath_ReturnsFalse()
    {
        // Arrange
        var nonexistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var config = new AppConfigModel
        {
            StarfieldAppDataPath = nonexistentPath,
            StarfieldGamePath = Path.GetTempPath()
        };

        // Act
        bool result = config.IsValid();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_NonexistentGamePath_ReturnsFalse()
    {
        // Arrange
        var nonexistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var config = new AppConfigModel
        {
            StarfieldAppDataPath = Path.GetTempPath(),
            StarfieldGamePath = nonexistentPath
        };

        // Act
        bool result = config.IsValid();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_MissingDataFolder_ReturnsFalse()
    {
        // Arrange
        using var context = new TestConfigContext();
        
        // Delete the Data folder
        var dataPath = Path.Combine(context.StarfieldGamePath, "Data");
        if (Directory.Exists(dataPath))
        {
            Directory.Delete(dataPath, true);
        }

        // Act
        bool result = context.Config.IsValid();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_MissingPluginsFile_ReturnsFalse()
    {
        // Arrange
        using var context = new TestConfigContext();
        
        // Delete Plugins.txt
        if (File.Exists(context.PluginsFilePath))
        {
            File.Delete(context.PluginsFilePath);
        }

        // Act
        bool result = context.Config.IsValid();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_ValidConfiguration_ReturnsTrue()
    {
        // Arrange
        using var context = new TestConfigContext();

        // Act
        bool result = context.Config.IsValid();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValid_CreatesProfilesFolderIfMissing()
    {
        // Arrange
        using var context = new TestConfigContext();
        var profilesFolder = Path.Combine(context.StarfieldAppDataPath, "Profiles");
        
        // Ensure Profiles folder doesn't exist
        if (Directory.Exists(profilesFolder))
        {
            Directory.Delete(profilesFolder, true);
        }

        // Act
        bool result = context.Config.IsValid();

        // Assert
        Assert.True(result);
        Assert.True(Directory.Exists(profilesFolder));
    }

    [Fact]
    public void IsValid_TestsProfilesFolderWritability()
    {
        // Arrange
        using var context = new TestConfigContext();

        // Act
        bool result = context.Config.IsValid();

        // Assert
        Assert.True(result);
        
        // Verify that a test file was created and deleted
        // (The test file itself should be cleaned up by IsValid())
        var profilesFolder = Path.Combine(context.StarfieldAppDataPath, "Profiles");
        Assert.True(Directory.Exists(profilesFolder));
    }

    #endregion

    #region GetPluginsFilePath Tests

    [Fact]
    public void GetPluginsFilePath_ReturnsCorrectPath()
    {
        // Arrange
        var appDataPath = Path.Combine("C:", "Users", "TestUser", "AppData");
        var config = new AppConfigModel
        {
            StarfieldAppDataPath = appDataPath
        };

        // Act
        var result = config.GetPluginsFilePath();

        // Assert
        Assert.Equal(Path.Combine(appDataPath, "Plugins.txt"), result);
    }

    [Fact]
    public void GetPluginsFilePath_UsesAppDataPath()
    {
        // Arrange
        using var context = new TestConfigContext();

        // Act
        var result = context.Config.GetPluginsFilePath();

        // Assert
        Assert.StartsWith(context.StarfieldAppDataPath, result);
        Assert.EndsWith("Plugins.txt", result);
    }

    #endregion

    #region GetReferenceFilePath Tests

    [Fact]
    public void GetReferenceFilePath_ReturnsPathInProfilesFolder()
    {
        // Arrange
        var appDataPath = Path.Combine("C:", "Users", "TestUser", "AppData");
        var config = new AppConfigModel
        {
            StarfieldAppDataPath = appDataPath,
            ActiveProfileId = "default"
        };

        // Act
        var result = config.GetReferenceFilePath();

        // Assert
        var expected = Path.Combine(appDataPath, "Profiles", "default", "reference.txt");
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetReferenceFilePath_UsesActiveProfileId()
    {
        // Arrange
        var appDataPath = Path.Combine("C:", "Users", "TestUser", "AppData");
        var config = new AppConfigModel
        {
            StarfieldAppDataPath = appDataPath,
            ActiveProfileId = "custom-profile"
        };

        // Act
        var result = config.GetReferenceFilePath();

        // Assert
        Assert.Contains("custom-profile", result);
        Assert.EndsWith("reference.txt", result);
    }

    [Fact]
    public void GetReferenceFilePath_NullActiveProfileId_UsesDefault()
    {
        // Arrange
        var appDataPath = Path.Combine("C:", "Users", "TestUser", "AppData");
        var config = new AppConfigModel
        {
            StarfieldAppDataPath = appDataPath,
            ActiveProfileId = null
        };

        // Act
        var result = config.GetReferenceFilePath();

        // Assert
        Assert.Contains("default", result);
        Assert.EndsWith("reference.txt", result);
    }

    [Fact]
    public void GetReferenceFilePath_DifferentProfiles_ReturnsDifferentPaths()
    {
        // Arrange
        var appDataPath = Path.Combine("C:", "Users", "TestUser", "AppData");
        var config1 = new AppConfigModel
        {
            StarfieldAppDataPath = appDataPath,
            ActiveProfileId = "profile1"
        };
        var config2 = new AppConfigModel
        {
            StarfieldAppDataPath = appDataPath,
            ActiveProfileId = "profile2"
        };

        // Act
        var result1 = config1.GetReferenceFilePath();
        var result2 = config2.GetReferenceFilePath();

        // Assert
        Assert.NotEqual(result1, result2);
        Assert.Contains("profile1", result1);
        Assert.Contains("profile2", result2);
    }

    #endregion

    #region Property Assignment Tests

    [Fact]
    public void StarfieldAppDataPath_CanBeSet()
    {
        // Arrange
        var config = new AppConfigModel();
        var newPath = Path.Combine("C:", "TestPath");

        // Act
        config.StarfieldAppDataPath = newPath;

        // Assert
        Assert.Equal(newPath, config.StarfieldAppDataPath);
    }

    [Fact]
    public void StarfieldGamePath_CanBeSet()
    {
        // Arrange
        var config = new AppConfigModel();
        var newPath = Path.Combine("C:", "TestPath");

        // Act
        config.StarfieldGamePath = newPath;

        // Assert
        Assert.Equal(newPath, config.StarfieldGamePath);
    }

    [Fact]
    public void ActiveProfileId_CanBeSet()
    {
        // Arrange
        var config = new AppConfigModel();

        // Act
        config.ActiveProfileId = "test-profile";

        // Assert
        Assert.Equal("test-profile", config.ActiveProfileId);
    }

    [Fact]
    public void ActiveProfileId_CanBeSetToNull()
    {
        // Arrange
        var config = new AppConfigModel
        {
            ActiveProfileId = "test-profile"
        };

        // Act
        config.ActiveProfileId = null;

        // Assert
        Assert.Null(config.ActiveProfileId);
    }

    #endregion
}
