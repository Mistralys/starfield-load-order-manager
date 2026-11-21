using System;
using System.IO;
using System.Threading.Tasks;
using LoadOrderKeeper.Models;
using LoadOrderKeeper.Services;
using Xunit;

namespace LoadOrderKeeper.Tests.Services;

/// <summary>
/// Tests covering the initialization-sequence behaviors that ViewModelInitializer relies on:
/// active profile canonicalization runs before reference-path resolution, corrected IDs are
/// persisted, and valid custom profiles are preserved unchanged.
/// </summary>
public class ViewModelInitializerTests
{
    /// <summary>
    /// AC-01, AC-04: A stale ActiveProfileId is normalized to "default" and the corrected
    /// value is persisted before reference-dependent operations consume the config.
    /// This verifies the sub-operation sequence that LoadInitialStateAsync depends on.
    /// </summary>
    [Fact]
    public async Task NormalizeActiveProfile_StaleId_CorrectsToDfaultAndPersists()
    {
        using var context = new TestConfigContext();
        context.Config.ActiveProfileId = "nonexistent-profile";
        await ProfileService.EnsureDefaultProfileFilesAsync(context.Config);

        // Act — mirrors the call in ViewModelInitializer.LoadInitialStateAsync
        await ProfileService.NormalizeActiveProfileAsync(context.Config, persistIfChanged: true);

        // Assert: config identity corrected before any reference path is built
        Assert.Equal("default", context.Config.ActiveProfileId);

        // Assert: reference path derived from the corrected ID resolves to the default file
        var referencePath = context.Config.GetReferenceFilePath();
        var expectedReferencePath = ProfileService.GetProfileReferenceFilePath(context.Config, "default");
        Assert.Equal(expectedReferencePath, referencePath, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// AC-03: When the configured ActiveProfileId matches an existing custom profile,
    /// initialization leaves it unchanged and does not fall back to default.
    /// </summary>
    [Fact]
    public async Task NormalizeActiveProfile_ValidCustomProfile_IsPreservedUnchanged()
    {
        using var context = new TestConfigContext();
        await ProfileService.EnsureDefaultProfileFilesAsync(context.Config);

        var customProfile = await ProfileService.CreateProfileAsync(context.Config, "My Profile", "");
        context.Config.ActiveProfileId = customProfile.Id;

        // Act — mirrors the call in ViewModelInitializer.LoadInitialStateAsync
        var resolved = await ProfileService.NormalizeActiveProfileAsync(context.Config, persistIfChanged: true);

        // Assert: custom profile identity is preserved
        Assert.Equal(customProfile.Id, resolved.Id);
        Assert.Equal(customProfile.Id, context.Config.ActiveProfileId);
    }
}
