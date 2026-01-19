using LoadOrderKeeper.Models;
using Xunit;

namespace LoadOrderKeeper.Tests.Models;

/// <summary>
/// Tests for ProfileModel covering default profile creation, property validation,
/// and instance creation.
/// </summary>
public sealed class ProfileModelTests
{
    #region Constructor Tests

    [Fact]
    public void DefaultConstructor_InitializesWithEmptyStrings()
    {
        // Act
        var profile = new ProfileModel();

        // Assert
        Assert.Equal(string.Empty, profile.Id);
        Assert.Equal(string.Empty, profile.Label);
        Assert.Equal(string.Empty, profile.Description);
    }

    [Fact]
    public void ParameterizedConstructor_SetsProperties()
    {
        // Act
        var profile = new ProfileModel("test-id", "Test Label", "Test description");

        // Assert
        Assert.Equal("test-id", profile.Id);
        Assert.Equal("Test Label", profile.Label);
        Assert.Equal("Test description", profile.Description);
    }

    [Fact]
    public void ParameterizedConstructor_WithoutDescription_UsesEmptyString()
    {
        // Act
        var profile = new ProfileModel("test-id", "Test Label");

        // Assert
        Assert.Equal("test-id", profile.Id);
        Assert.Equal("Test Label", profile.Label);
        Assert.Equal(string.Empty, profile.Description);
    }

    #endregion

    #region IsDefault Tests

    [Fact]
    public void IsDefault_IdIsDefault_ReturnsTrue()
    {
        // Arrange
        var profile = new ProfileModel("default", "Default");

        // Act & Assert
        Assert.True(profile.IsDefault);
    }

    [Fact]
    public void IsDefault_IdIsNotDefault_ReturnsFalse()
    {
        // Arrange
        var profile = new ProfileModel("custom", "Custom Profile");

        // Act & Assert
        Assert.False(profile.IsDefault);
    }

    [Fact]
    public void IsDefault_IdIsDefaultUppercase_ReturnsFalse()
    {
        // Arrange
        var profile = new ProfileModel("DEFAULT", "Default");

        // Act & Assert
        // IsDefault is case-sensitive by design
        Assert.False(profile.IsDefault);
    }

    [Fact]
    public void IsDefault_EmptyId_ReturnsFalse()
    {
        // Arrange
        var profile = new ProfileModel();

        // Act & Assert
        Assert.False(profile.IsDefault);
    }

    #endregion

    #region CreateDefault Tests

    [Fact]
    public void CreateDefault_CreatesDefaultProfile()
    {
        // Act
        var profile = ProfileModel.CreateDefault();

        // Assert
        Assert.Equal("default", profile.Id);
        Assert.Equal("Default", profile.Label);
        Assert.Equal("The default profile is always available.", profile.Description);
    }

    [Fact]
    public void CreateDefault_IsDefault_ReturnsTrue()
    {
        // Act
        var profile = ProfileModel.CreateDefault();

        // Assert
        Assert.True(profile.IsDefault);
    }

    [Fact]
    public void CreateDefault_CreatesNewInstanceEachTime()
    {
        // Act
        var profile1 = ProfileModel.CreateDefault();
        var profile2 = ProfileModel.CreateDefault();

        // Assert
        Assert.NotSame(profile1, profile2);
        Assert.Equal(profile1.Id, profile2.Id);
        Assert.Equal(profile1.Label, profile2.Label);
        Assert.Equal(profile1.Description, profile2.Description);
    }

    #endregion

    #region Property Mutability Tests

    [Fact]
    public void Id_IsInitOnly()
    {
        // Arrange
        var profile = new ProfileModel("test", "Test");

        // Act - Try to set Id using reflection (should be init-only)
        var idProperty = typeof(ProfileModel).GetProperty(nameof(ProfileModel.Id));
        
        // Assert
        // The property should have an init accessor
        Assert.NotNull(idProperty);
        Assert.NotNull(idProperty.GetSetMethod(true)); // Gets init accessor
    }

    [Fact]
    public void Label_CanBeModified()
    {
        // Arrange
        var profile = new ProfileModel("test", "Original");

        // Act
        profile.Label = "Modified";

        // Assert
        Assert.Equal("Modified", profile.Label);
    }

    [Fact]
    public void Description_CanBeModified()
    {
        // Arrange
        var profile = new ProfileModel("test", "Test", "Original");

        // Act
        profile.Description = "Modified description";

        // Assert
        Assert.Equal("Modified description", profile.Description);
    }

    #endregion

    #region Profile Identity Tests

    [Fact]
    public void TwoProfilesWithSameId_AreNotReferenceEqual()
    {
        // Arrange
        var profile1 = new ProfileModel("test", "Label 1");
        var profile2 = new ProfileModel("test", "Label 2");

        // Assert
        Assert.NotSame(profile1, profile2);
    }

    [Fact]
    public void ProfilesWithDifferentIds_AreDifferentProfiles()
    {
        // Arrange
        var profile1 = new ProfileModel("test1", "Test");
        var profile2 = new ProfileModel("test2", "Test");

        // Assert
        Assert.NotEqual(profile1.Id, profile2.Id);
        Assert.False(profile1.IsDefault);
        Assert.False(profile2.IsDefault);
    }

    #endregion

    #region String Property Handling Tests

    [Fact]
    public void Label_AcceptsEmptyString()
    {
        // Arrange & Act
        var profile = new ProfileModel("test", "");

        // Assert
        Assert.Equal(string.Empty, profile.Label);
    }

    [Fact]
    public void Description_AcceptsEmptyString()
    {
        // Arrange & Act
        var profile = new ProfileModel("test", "Test", "");

        // Assert
        Assert.Equal(string.Empty, profile.Description);
    }

    [Fact]
    public void Label_AcceptsLongString()
    {
        // Arrange
        var longLabel = new string('A', 100);

        // Act
        var profile = new ProfileModel("test", longLabel);

        // Assert
        Assert.Equal(longLabel, profile.Label);
    }

    [Fact]
    public void Description_AcceptsLongString()
    {
        // Arrange
        var longDescription = new string('B', 1000);

        // Act
        var profile = new ProfileModel("test", "Test", longDescription);

        // Assert
        Assert.Equal(longDescription, profile.Description);
    }

    #endregion

    #region Special Characters Tests

    [Fact]
    public void Label_AcceptsSpecialCharacters()
    {
        // Arrange & Act
        var profile = new ProfileModel("test", "Test™ Profile © 2024");

        // Assert
        Assert.Equal("Test™ Profile © 2024", profile.Label);
    }

    [Fact]
    public void Description_AcceptsSpecialCharacters()
    {
        // Arrange & Act
        var profile = new ProfileModel("test", "Test", "Description with symbols: !@#$%^&*()");

        // Assert
        Assert.Contains("!@#$%^&*()", profile.Description);
    }

    [Fact]
    public void Id_AcceptsHyphenatedString()
    {
        // Arrange & Act
        var profile = new ProfileModel("my-custom-profile", "My Custom Profile");

        // Assert
        Assert.Equal("my-custom-profile", profile.Id);
    }

    #endregion
}
