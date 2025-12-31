using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LoadOrderKeeper.Services;
using Xunit;

namespace LoadOrderKeeper.Tests;

public sealed class ProfileServiceTests
{
    [Fact]
    public async Task LoadProfiles_NoProfiles_ReturnsDefaultOnly()
    {
        using var context = new TestConfigContext();

        var profiles = await ProfileService.LoadProfilesAsync(context.Config);

        Assert.Single(profiles);
        Assert.Equal("default", profiles[0].Id);
        Assert.Equal("Default", profiles[0].Label);
        Assert.True(profiles[0].IsDefault);
    }

    [Fact]
    public async Task CreateProfile_ValidInput_CreatesProfileWithFiles()
    {
        using var context = new TestConfigContext();
        await context.WritePluginsAsync("*TestMod.esp");

        var profile = await ProfileService.CreateProfileAsync(context.Config, "Test Profile", "Test description");

        Assert.Equal("test-profile", profile.Id);
        Assert.Equal("Test Profile", profile.Label);
        Assert.Equal("Test description", profile.Description);

        var profileFolder = ProfileService.GetProfileFolder(context.Config, profile.Id);
        Assert.True(Directory.Exists(profileFolder));
        Assert.True(File.Exists(Path.Combine(profileFolder, "profile.json")));
        Assert.True(File.Exists(Path.Combine(profileFolder, "main.txt")));
        Assert.True(File.Exists(Path.Combine(profileFolder, "reference.txt")));
    }

    [Fact]
    public async Task LoadProfiles_WithCustomProfiles_ReturnsSortedList()
    {
        using var context = new TestConfigContext();
        await context.WritePluginsAsync("*TestMod.esp");

        await ProfileService.CreateProfileAsync(context.Config, "Zebra", "");
        await ProfileService.CreateProfileAsync(context.Config, "Alpha", "");
        await ProfileService.CreateProfileAsync(context.Config, "Beta", "");

        var profiles = await ProfileService.LoadProfilesAsync(context.Config);

        Assert.Equal(4, profiles.Count);
        Assert.Equal("default", profiles[0].Id);
        Assert.Equal("Alpha", profiles[1].Label);
        Assert.Equal("Beta", profiles[2].Label);
        Assert.Equal("Zebra", profiles[3].Label);
    }

    [Fact]
    public async Task GenerateProfileId_SimpleLabel_ReturnsLowercaseDashed()
    {
        var existingIds = new System.Collections.Generic.HashSet<string>();

        var id = ProfileService.GenerateProfileId("My Test Profile", existingIds);

        Assert.Equal("my-test-profile", id);
    }

    [Fact]
    public async Task GenerateProfileId_DuplicateLabel_AppendsNumericSuffix()
    {
        var existingIds = new System.Collections.Generic.HashSet<string> { "test", "test-1" };

        var id = ProfileService.GenerateProfileId("Test", existingIds);

        Assert.Equal("test-2", id);
    }

    [Fact]
    public async Task GenerateProfileId_AccentedCharacters_RemovesAccents()
    {
        var existingIds = new System.Collections.Generic.HashSet<string>();

        var id = ProfileService.GenerateProfileId("Élite Café", existingIds);

        Assert.Equal("elite-cafe", id);
    }

    [Fact]
    public async Task GenerateProfileId_EmptyAfterTransliteration_UsesFallback()
    {
        var existingIds = new System.Collections.Generic.HashSet<string>();

        var id = ProfileService.GenerateProfileId("???", existingIds);

        Assert.Equal("profile", id);
    }

    [Fact]
    public async Task UpdateProfile_ChangesLabel_RenamesFolder()
    {
        using var context = new TestConfigContext();
        await context.WritePluginsAsync("*TestMod.esp");

        var profile = await ProfileService.CreateProfileAsync(context.Config, "Old Name", "Test");
        var oldFolder = ProfileService.GetProfileFolder(context.Config, profile.Id);
        Assert.True(Directory.Exists(oldFolder));

        await ProfileService.UpdateProfileAsync(context.Config, profile, "New Name", "Updated");

        var newFolder = ProfileService.GetProfileFolder(context.Config, "new-name");
        Assert.True(Directory.Exists(newFolder));
        Assert.False(Directory.Exists(oldFolder));

        var profiles = await ProfileService.LoadProfilesAsync(context.Config);
        var updatedProfile = profiles.FirstOrDefault(p => p.Id == "new-name");
        Assert.NotNull(updatedProfile);
        Assert.Equal("New Name", updatedProfile.Label);
        Assert.Equal("Updated", updatedProfile.Description);
    }

    [Fact]
    public async Task DeleteProfile_ExistingProfile_RemovesFolder()
    {
        using var context = new TestConfigContext();
        await context.WritePluginsAsync("*TestMod.esp");

        var profile = await ProfileService.CreateProfileAsync(context.Config, "To Delete", "");
        var profileFolder = ProfileService.GetProfileFolder(context.Config, profile.Id);
        Assert.True(Directory.Exists(profileFolder));

        await ProfileService.DeleteProfileAsync(context.Config, profile.Id);

        Assert.False(Directory.Exists(profileFolder));

        var profiles = await ProfileService.LoadProfilesAsync(context.Config);
        Assert.Single(profiles); // Only default remains
    }

    [Fact]
    public async Task DeleteProfile_DefaultProfile_ThrowsException()
    {
        using var context = new TestConfigContext();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => ProfileService.DeleteProfileAsync(context.Config, "default"));
    }

    [Fact]
    public async Task CopyProfile_ValidProfile_CreatesNewProfileWithCopiedFiles()
    {
        using var context = new TestConfigContext();
        await context.WritePluginsAsync("*TestMod.esp");

        var sourceProfile = await ProfileService.CreateProfileAsync(context.Config, "Source", "Original");
        
        var copiedProfile = await ProfileService.CopyProfileAsync(context.Config, sourceProfile.Id, "Copy");

        Assert.Equal("copy", copiedProfile.Id);
        Assert.Equal("Copy", copiedProfile.Label);
        Assert.Equal("Original", copiedProfile.Description);

        var copiedFolder = ProfileService.GetProfileFolder(context.Config, copiedProfile.Id);
        Assert.True(Directory.Exists(copiedFolder));
        Assert.True(File.Exists(Path.Combine(copiedFolder, "profile.json")));
        Assert.True(File.Exists(Path.Combine(copiedFolder, "main.txt")));
        Assert.True(File.Exists(Path.Combine(copiedFolder, "reference.txt")));
    }

    [Fact]
    public async Task SwitchProfile_ValidProfile_UpdatesPluginsAndConfig()
    {
        using var context = new TestConfigContext();
        await context.WritePluginsAsync("*DefaultMod.esp");

        var newProfile = await ProfileService.CreateProfileAsync(context.Config, "New Profile", "");
        var newProfileMainPath = ProfileService.GetProfileMainFilePath(context.Config, newProfile.Id);
        await File.WriteAllTextAsync(newProfileMainPath, "*NewProfileMod.esp\n");

        await ProfileService.SwitchProfileAsync(context.Config, newProfile.Id);

        Assert.Equal(newProfile.Id, context.Config.ActiveProfileId);

        var pluginsContent = await File.ReadAllTextAsync(context.PluginsFilePath);
        Assert.Contains("NewProfileMod.esp", pluginsContent);

        // Verify old profile's main.txt was backed up
        var defaultMainPath = ProfileService.GetProfileMainFilePath(context.Config, "default");
        var defaultContent = await File.ReadAllTextAsync(defaultMainPath);
        Assert.Contains("DefaultMod.esp", defaultContent);
    }

    [Fact]
    public async Task GetActiveProfile_ValidActiveId_ReturnsCorrectProfile()
    {
        using var context = new TestConfigContext();
        await context.WritePluginsAsync("*TestMod.esp");

        var profile = await ProfileService.CreateProfileAsync(context.Config, "Active Profile", "");
        context.Config.ActiveProfileId = profile.Id;

        var activeProfile = await ProfileService.GetActiveProfileAsync(context.Config);

        Assert.Equal(profile.Id, activeProfile.Id);
        Assert.Equal(profile.Label, activeProfile.Label);
    }

    [Fact]
    public async Task GetActiveProfile_InvalidActiveId_ReturnsDefault()
    {
        using var context = new TestConfigContext();
        context.Config.ActiveProfileId = "nonexistent";

        var activeProfile = await ProfileService.GetActiveProfileAsync(context.Config);

        Assert.Equal("default", activeProfile.Id);
        Assert.True(activeProfile.IsDefault);
    }

    [Fact]
    public async Task EnsureDefaultProfileFiles_MissingFiles_CreatesFromPlugins()
    {
        using var context = new TestConfigContext();
        await context.WritePluginsAsync("*TestMod.esp");

        await ProfileService.EnsureDefaultProfileFilesAsync(context.Config);

        var defaultFolder = ProfileService.GetProfileFolder(context.Config, "default");
        Assert.True(Directory.Exists(defaultFolder));

        var mainPath = ProfileService.GetProfileMainFilePath(context.Config, "default");
        Assert.True(File.Exists(mainPath));

        var refPath = ProfileService.GetProfileReferenceFilePath(context.Config, "default");
        Assert.True(File.Exists(refPath));

        var mainContent = await File.ReadAllTextAsync(mainPath);
        Assert.Contains("TestMod.esp", mainContent);
    }
}
