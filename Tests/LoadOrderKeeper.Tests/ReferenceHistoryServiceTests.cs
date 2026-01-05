using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LoadOrderKeeper.Models;
using LoadOrderKeeper.Services;
using Xunit;

namespace LoadOrderKeeper.Tests;

public sealed class ReferenceHistoryServiceTests
{
    [Fact]
    public async Task LoadVersionHistory_NoHistory_ReturnsEmptyList()
    {
        using var context = new TestConfigContext();

        var versions = await ReferenceHistoryService.LoadVersionHistoryAsync(context.Config);

        Assert.Empty(versions);
    }

    [Fact]
    public async Task ArchiveCurrentReference_FirstVersion_CreatesInitialVersion()
    {
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*ModA.esp", "*ModB.esp");

        var versionNumber = await ReferenceHistoryService.ArchiveCurrentReferenceAsync(
            context.Config,
            null,
            new List<string>(),
            new List<string>());

        Assert.Equal(1, versionNumber);

        var versions = await ReferenceHistoryService.LoadVersionHistoryAsync(context.Config);
        Assert.Single(versions);
        Assert.Equal(1, versions[0].VersionNumber);
        Assert.Equal("Initial version", versions[0].Comment);
        Assert.Empty(versions[0].AddedMods);
        Assert.Empty(versions[0].RemovedMods);

        var historyFolder = ReferenceHistoryService.GetHistoryFolder(context.Config);
        Assert.True(File.Exists(Path.Combine(historyFolder, "reference_v1.txt")));
        Assert.True(File.Exists(Path.Combine(historyFolder, "reference_v1.json")));
    }

    [Fact]
    public async Task ArchiveCurrentReference_WithComment_UsesComment()
    {
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*ModA.esp");

        await ReferenceHistoryService.ArchiveCurrentReferenceAsync(
            context.Config,
            "My custom comment",
            new List<string>(),
            new List<string>());

        var versions = await ReferenceHistoryService.LoadVersionHistoryAsync(context.Config);
        Assert.Equal("My custom comment", versions[0].Comment);
    }

    [Fact]
    public async Task ArchiveCurrentReference_WithChanges_RecordsChanges()
    {
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*ModA.esp", "*ModB.esp");

        // First version (initial)
        await ReferenceHistoryService.ArchiveCurrentReferenceAsync(
            context.Config,
            null,
            new List<string>(),
            new List<string>());

        // Second version with changes
        var addedMods = new List<string> { "ModC.esp", "ModD.esp" };
        var removedMods = new List<string> { "ModA.esp" };

        await ReferenceHistoryService.ArchiveCurrentReferenceAsync(
            context.Config,
            "Added mods C and D, removed A",
            addedMods,
            removedMods);

        var versions = await ReferenceHistoryService.LoadVersionHistoryAsync(context.Config);
        Assert.Equal(2, versions.Count);

        var version2 = versions.First(v => v.VersionNumber == 2);
        Assert.Equal(2, version2.AddedMods.Count);
        Assert.Contains("ModC.esp", version2.AddedMods);
        Assert.Contains("ModD.esp", version2.AddedMods);
        Assert.Single(version2.RemovedMods);
        Assert.Contains("ModA.esp", version2.RemovedMods);
    }

    [Fact]
    public async Task ArchiveCurrentReference_SequentialVersions_IncrementsVersionNumber()
    {
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*ModA.esp");

        await ReferenceHistoryService.ArchiveCurrentReferenceAsync(context.Config, "V1", new List<string>(), new List<string>());
        await ReferenceHistoryService.ArchiveCurrentReferenceAsync(context.Config, "V2", new List<string>(), new List<string>());
        await ReferenceHistoryService.ArchiveCurrentReferenceAsync(context.Config, "V3", new List<string>(), new List<string>());

        var versions = await ReferenceHistoryService.LoadVersionHistoryAsync(context.Config);

        Assert.Equal(3, versions.Count);
        // Versions are returned in descending order (newest first)
        Assert.Equal(3, versions[0].VersionNumber);
        Assert.Equal(2, versions[1].VersionNumber);
        Assert.Equal(1, versions[2].VersionNumber);
    }

    [Fact]
    public async Task ArchiveCurrentReference_ArchivesReferenceFileContent()
    {
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*ModA.esp", "*ModB.esp", "*ModC.esp");

        await ReferenceHistoryService.ArchiveCurrentReferenceAsync(
            context.Config,
            "Test",
            new List<string>(),
            new List<string>());

        var historyFolder = ReferenceHistoryService.GetHistoryFolder(context.Config);
        var archivedContent = await File.ReadAllLinesAsync(Path.Combine(historyFolder, "reference_v1.txt"));

        Assert.Equal(3, archivedContent.Length);
        Assert.Contains("*ModA.esp", archivedContent);
        Assert.Contains("*ModB.esp", archivedContent);
        Assert.Contains("*ModC.esp", archivedContent);
    }

    [Fact]
    public async Task LoadVersionHistory_MultipleVersions_ReturnsSortedByVersionNumber()
    {
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*ModA.esp");

        // Create versions out of order (shouldn't happen in practice, but test for robustness)
        await ReferenceHistoryService.ArchiveCurrentReferenceAsync(context.Config, "V1", new List<string>(), new List<string>());
        await ReferenceHistoryService.ArchiveCurrentReferenceAsync(context.Config, "V2", new List<string>(), new List<string>());
        await ReferenceHistoryService.ArchiveCurrentReferenceAsync(context.Config, "V3", new List<string>(), new List<string>());

        var versions = await ReferenceHistoryService.LoadVersionHistoryAsync(context.Config);

        Assert.Equal(3, versions.Count);
        // Versions returned in descending order (newest first)
        Assert.True(versions[0].VersionNumber > versions[1].VersionNumber);
        Assert.True(versions[1].VersionNumber > versions[2].VersionNumber);
    }

    [Fact]
    public async Task RollbackToVersion_ValidVersion_RestoresReferenceFile()
    {
        using var context = new TestConfigContext();
        
        // Create initial reference
        await context.WriteReferenceAsync("*ModA.esp", "*ModB.esp");
        await ReferenceHistoryService.ArchiveCurrentReferenceAsync(context.Config, "V1", new List<string>(), new List<string>());

        // Modify reference and archive again
        await context.WriteReferenceAsync("*ModA.esp", "*ModB.esp", "*ModC.esp");
        await ReferenceHistoryService.ArchiveCurrentReferenceAsync(context.Config, "V2", new List<string> { "ModC.esp" }, new List<string>());

        // Rollback to version 1 (note: rollback copies to Plugins.txt, not reference.txt)
        await ReferenceHistoryService.RollbackToVersionAsync(context.Config, 1);

        // Check Plugins.txt (not reference.txt - rollback is for review in diff window)
        var pluginsContent = await File.ReadAllLinesAsync(context.PluginsFilePath);
        Assert.Equal(2, pluginsContent.Length);
        Assert.Contains("*ModA.esp", pluginsContent);
        Assert.Contains("*ModB.esp", pluginsContent);
        Assert.DoesNotContain("*ModC.esp", pluginsContent);
    }

    [Fact]
    public async Task RollbackToVersion_NonexistentVersion_ThrowsException()
    {
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*ModA.esp");

        await Assert.ThrowsAsync<FileNotFoundException>(
            () => ReferenceHistoryService.RollbackToVersionAsync(context.Config, 99));
    }

    [Fact]
    public async Task DeleteVersion_ValidVersion_RemovesFiles()
    {
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*ModA.esp");

        await ReferenceHistoryService.ArchiveCurrentReferenceAsync(context.Config, "V1", new List<string>(), new List<string>());
        await ReferenceHistoryService.ArchiveCurrentReferenceAsync(context.Config, "V2", new List<string>(), new List<string>());

        await ReferenceHistoryService.DeleteVersionAsync(context.Config, 1);

        var versions = await ReferenceHistoryService.LoadVersionHistoryAsync(context.Config);
        Assert.Single(versions);
        Assert.Equal(2, versions[0].VersionNumber);

        var historyFolder = ReferenceHistoryService.GetHistoryFolder(context.Config);
        Assert.False(File.Exists(Path.Combine(historyFolder, "reference_v1.txt")));
        Assert.False(File.Exists(Path.Combine(historyFolder, "reference_v1.json")));
    }

    [Fact]
    public async Task ClearHistory_MultipleVersions_RemovesAllFiles()
    {
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*ModA.esp");

        await ReferenceHistoryService.ArchiveCurrentReferenceAsync(context.Config, "V1", new List<string>(), new List<string>());
        await ReferenceHistoryService.ArchiveCurrentReferenceAsync(context.Config, "V2", new List<string>(), new List<string>());
        await ReferenceHistoryService.ArchiveCurrentReferenceAsync(context.Config, "V3", new List<string>(), new List<string>());

        await ReferenceHistoryService.ClearHistoryAsync(context.Config);

        var versions = await ReferenceHistoryService.LoadVersionHistoryAsync(context.Config);
        Assert.Empty(versions);

        var historyFolder = ReferenceHistoryService.GetHistoryFolder(context.Config);
        Assert.False(Directory.Exists(historyFolder) && Directory.GetFiles(historyFolder).Length > 0);
    }

    [Fact]
    public async Task UpdateVersionComment_ValidVersion_UpdatesComment()
    {
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*ModA.esp");

        await ReferenceHistoryService.ArchiveCurrentReferenceAsync(context.Config, "Original comment", new List<string>(), new List<string>());

        await ReferenceHistoryService.UpdateVersionCommentAsync(context.Config, 1, "Updated comment");

        var versions = await ReferenceHistoryService.LoadVersionHistoryAsync(context.Config);
        Assert.Equal("Updated comment", versions[0].Comment);
    }

    [Fact]
    public async Task UpdateVersionComment_NullComment_UpdatesToNull()
    {
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*ModA.esp");

        await ReferenceHistoryService.ArchiveCurrentReferenceAsync(context.Config, "Original comment", new List<string>(), new List<string>());

        await ReferenceHistoryService.UpdateVersionCommentAsync(context.Config, 1, null);

        var versions = await ReferenceHistoryService.LoadVersionHistoryAsync(context.Config);
        Assert.Null(versions[0].Comment);
    }

    [Fact]
    public async Task LoadPendingChanges_NoPendingChanges_ReturnsEmpty()
    {
        using var context = new TestConfigContext();

        var pendingChanges = await ReferenceHistoryService.LoadPendingChangesAsync(context.Config);

        Assert.True(pendingChanges.IsEmpty);
        Assert.Empty(pendingChanges.AddedMods);
        Assert.Empty(pendingChanges.RemovedMods);
    }

    [Fact]
    public async Task SaveAndLoadPendingChanges_ValidChanges_PersistsCorrectly()
    {
        using var context = new TestConfigContext();

        var changes = PendingChangesModel.Create(
            new List<string> { "ModX.esp", "ModY.esp" },
            new List<string> { "ModZ.esp" });

        await ReferenceHistoryService.SavePendingChangesAsync(context.Config, changes);

        var loaded = await ReferenceHistoryService.LoadPendingChangesAsync(context.Config);

        Assert.False(loaded.IsEmpty);
        Assert.Equal(2, loaded.AddedMods.Count);
        Assert.Contains("ModX.esp", loaded.AddedMods);
        Assert.Contains("ModY.esp", loaded.AddedMods);
        Assert.Single(loaded.RemovedMods);
        Assert.Contains("ModZ.esp", loaded.RemovedMods);
        Assert.Equal(3, loaded.TotalChanges);
    }

    [Fact]
    public async Task ClearPendingChanges_ExistingChanges_RemovesFile()
    {
        using var context = new TestConfigContext();

        var changes = PendingChangesModel.Create(
            new List<string> { "ModX.esp" },
            new List<string>());
        await ReferenceHistoryService.SavePendingChangesAsync(context.Config, changes);

        var pendingChangesPath = ReferenceHistoryService.GetPendingChangesFilePath(context.Config);
        Assert.True(File.Exists(pendingChangesPath));

        await ReferenceHistoryService.ClearPendingChangesAsync(context.Config);

        Assert.False(File.Exists(pendingChangesPath));

        var loaded = await ReferenceHistoryService.LoadPendingChangesAsync(context.Config);
        Assert.True(loaded.IsEmpty);
    }

    [Fact]
    public async Task ArchiveCurrentReference_MaxVersionsExceeded_PrunesOldestVersions()
    {
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*ModA.esp");

        // Create 18 versions (exceeds the limit of 16)
        for (int i = 1; i <= 18; i++)
        {
            await ReferenceHistoryService.ArchiveCurrentReferenceAsync(
                context.Config,
                $"Version {i}",
                new List<string>(),
                new List<string>());
        }

        var versions = await ReferenceHistoryService.LoadVersionHistoryAsync(context.Config);

        // Should only have 16 versions
        Assert.Equal(16, versions.Count);

        // Should have versions 3-18 (oldest 2 pruned)
        Assert.Equal(3, versions.Min(v => v.VersionNumber));
        Assert.Equal(18, versions.Max(v => v.VersionNumber));

        // Verify files for versions 1 and 2 are deleted
        var historyFolder = ReferenceHistoryService.GetHistoryFolder(context.Config);
        Assert.False(File.Exists(Path.Combine(historyFolder, "reference_v1.txt")));
        Assert.False(File.Exists(Path.Combine(historyFolder, "reference_v1.json")));
        Assert.False(File.Exists(Path.Combine(historyFolder, "reference_v2.txt")));
        Assert.False(File.Exists(Path.Combine(historyFolder, "reference_v2.json")));
    }

    [Fact]
    public async Task GetChangeSummary_NoChanges_ReturnsNoChanges()
    {
        var metadata = new ReferenceVersionMetadataModel
        {
            AddedMods = new List<string>(),
            RemovedMods = new List<string>()
        };

        var summary = metadata.GetChangeSummary();

        Assert.Equal("No changes", summary);
    }

    [Fact]
    public async Task GetChangeSummary_FewChanges_ListsNames()
    {
        var metadata = new ReferenceVersionMetadataModel
        {
            AddedMods = new List<string> { "ModX.esp", "ModY.esp" },
            RemovedMods = new List<string> { "ModZ.esp" }
        };

        var summary = metadata.GetChangeSummary();

        Assert.Contains("Added ModX.esp and ModY.esp", summary);
        Assert.Contains("Removed ModZ.esp", summary);
    }

    [Fact]
    public async Task GetChangeSummary_ManyChanges_ShowsCounts()
    {
        var metadata = new ReferenceVersionMetadataModel
        {
            AddedMods = new List<string> { "Mod1.esp", "Mod2.esp", "Mod3.esp", "Mod4.esp" },
            RemovedMods = new List<string> { "OldMod1.esp", "OldMod2.esp", "OldMod3.esp", "OldMod4.esp", "OldMod5.esp" }
        };

        var summary = metadata.GetChangeSummary();

        Assert.Contains("Added 4 mods", summary);
        Assert.Contains("Removed 5 mods", summary);
    }

    [Fact]
    public async Task PendingChangesModel_CreateEmpty_ReturnsEmptyModel()
    {
        var model = PendingChangesModel.CreateEmpty();

        Assert.True(model.IsEmpty);
        Assert.Empty(model.AddedMods);
        Assert.Empty(model.RemovedMods);
        Assert.Equal(0, model.TotalChanges);
    }

    [Fact]
    public async Task PendingChangesModel_Create_CreatesModelWithChanges()
    {
        var addedMods = new List<string> { "ModA.esp", "ModB.esp" };
        var removedMods = new List<string> { "ModC.esp" };

        var model = PendingChangesModel.Create(addedMods, removedMods);

        Assert.False(model.IsEmpty);
        Assert.Equal(2, model.AddedMods.Count);
        Assert.Single(model.RemovedMods);
        Assert.Equal(3, model.TotalChanges);
    }

    [Fact]
    public async Task LoadPendingChanges_CorruptedFile_ReturnsEmpty()
    {
        using var context = new TestConfigContext();

        // Write corrupted JSON
        var pendingChangesPath = ReferenceHistoryService.GetPendingChangesFilePath(context.Config);
        var pendingChangesDir = Path.GetDirectoryName(pendingChangesPath);
        if (!string.IsNullOrEmpty(pendingChangesDir))
        {
            Directory.CreateDirectory(pendingChangesDir);
        }
        await File.WriteAllTextAsync(pendingChangesPath, "{ invalid json content }");

        var loaded = await ReferenceHistoryService.LoadPendingChangesAsync(context.Config);

        // Should gracefully handle corruption by returning empty
        Assert.True(loaded.IsEmpty);
    }

    [Fact]
    public async Task LoadVersionHistory_CorruptedMetadata_SkipsCorruptedVersion()
    {
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*ModA.esp");

        // Create a valid version
        await ReferenceHistoryService.ArchiveCurrentReferenceAsync(context.Config, "V1", new List<string>(), new List<string>());

        // Manually create a corrupted version 2 metadata file
        var historyFolder = ReferenceHistoryService.GetHistoryFolder(context.Config);
        await File.WriteAllTextAsync(Path.Combine(historyFolder, "reference_v2.json"), "{ corrupted json }");
        await File.WriteAllTextAsync(Path.Combine(historyFolder, "reference_v2.txt"), "*ModB.esp");

        var versions = await ReferenceHistoryService.LoadVersionHistoryAsync(context.Config);

        // Should only load the valid version
        Assert.Single(versions);
        Assert.Equal(1, versions[0].VersionNumber);
    }

    [Fact]
    public async Task ArchiveCurrentReference_EmptyHistoryAndEmptyPending_CreatesInitialVersionAutomatically()
    {
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*ModA.esp", "*ModB.esp");

        // Simulate calling with changes (but history is empty and no pending changes exist)
        var versionNumber = await ReferenceHistoryService.ArchiveCurrentReferenceAsync(
            context.Config,
            null,
            new List<string> { "ModC.esp" }, // These will be ignored for first version
            new List<string>());

        var versions = await ReferenceHistoryService.LoadVersionHistoryAsync(context.Config);
        
        // Should create initial version with no changes
        Assert.Single(versions);
        Assert.Equal("Initial version", versions[0].Comment);
        Assert.Empty(versions[0].AddedMods);
        Assert.Empty(versions[0].RemovedMods);
    }

    [Fact]
    public async Task GetHistoryFolder_ValidConfig_ReturnsCorrectPath()
    {
        using var context = new TestConfigContext();

        var historyFolder = ReferenceHistoryService.GetHistoryFolder(context.Config);

        var expectedPath = Path.Combine(
            ProfileService.GetProfileFolder(context.Config, "default"),
            "History");
        Assert.Equal(expectedPath, historyFolder);
    }

    [Fact]
    public async Task GetPendingChangesFilePath_ValidConfig_ReturnsCorrectPath()
    {
        using var context = new TestConfigContext();

        var pendingChangesPath = ReferenceHistoryService.GetPendingChangesFilePath(context.Config);

        var expectedPath = Path.Combine(
            ProfileService.GetProfileFolder(context.Config, "default"),
            "pending-changes.json");
        Assert.Equal(expectedPath, pendingChangesPath);
    }
}
