using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LoadOrderKeeper.Services;
using Xunit;

namespace LoadOrderKeeper.Tests;

public class FileServiceTests
{
    [Fact]
    public async Task HasPluginsFileChangedAsync_ReturnsFalse_WhenFilesMatch()
    {
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*a.esm", "*b.esm");
        await context.WritePluginsAsync("*a.esm", "*b.esm");

        bool changed = await FileService.HasPluginsFileChangedAsync(context.Config);

        Assert.False(changed);
    }

    [Fact]
    public async Task HasPluginsFileChangedAsync_ReturnsTrue_WhenFilesDiffer()
    {
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*a.esm", "*b.esm");
        await context.WritePluginsAsync("*a.esm", "*c.esm");

        bool changed = await FileService.HasPluginsFileChangedAsync(context.Config);

        Assert.True(changed);
    }

    [Fact]
    public async Task GetModDiffAsync_DetectsMovedMods()
    {
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*a.esm", "*b.esm");
        await context.WritePluginsAsync("*b.esm", "*a.esm");

        var diffs = await FileService.GetModDiffAsync(context.Config);

        var moved = diffs.Where(d => d.IsMoved).ToList();
        Assert.Equal(2, moved.Count);
        Assert.Contains(moved, d => d.FileName == "a.esm" && d.ReferenceNumber == 1 && d.CurrentNumber == 2);
        Assert.Contains(moved, d => d.FileName == "b.esm" && d.ReferenceNumber == 2 && d.CurrentNumber == 1);
    }

    [Fact]
    public async Task GetModDiffAsync_TreatsDisabledModsAsRemoved()
    {
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*a.esm", "*b.esm");
        await context.WritePluginsAsync("*a.esm", "b.esm");

        var diffs = await FileService.GetModDiffAsync(context.Config);

        var removed = diffs.First(d => d.FileName == "b.esm");
        Assert.True(removed.IsRemoved);
        Assert.Null(removed.CurrentNumber);
        Assert.Equal(2, removed.ReferenceNumber);
    }

    [Fact]
    public async Task GetModDiffAsync_IgnoresDisabledNewMods()
    {
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*a.esm");
        await context.WritePluginsAsync("*a.esm", "new.esm");

        var diffs = await FileService.GetModDiffAsync(context.Config);

        Assert.DoesNotContain(diffs, d => d.FileName == "new.esm");
    }

    [Fact]
    public async Task ApplyLoadOrderAsync_WritesOnlyEnabledMods()
    {
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*a.esm");
        await context.WritePluginsAsync("*a.esm", "*b.esm", "c.esm");

        await FileService.ApplyLoadOrderAsync(context.Config);

        var lines = await File.ReadAllLinesAsync(context.PluginsFilePath);
        Assert.Equal(new[] { "*a.esm", "*b.esm" }, lines);
    }

    [Fact]
    public async Task HasDeletedModsAsync_ReturnsTrue_WhenReferenceEntryMissing()
    {
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*only.esm");
        await context.WritePluginsAsync();

        bool hasDeleted = await FileService.HasDeletedModsAsync(context.Config);

        Assert.True(hasDeleted);
    }

    [Fact]
    public async Task WouldSortingChangeDiffsAsync_ReturnsTrue_ForOrderChanges()
    {
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*a.esm", "*b.esm");
        await context.WritePluginsAsync("*b.esm", "*a.esm");

        bool result = await FileService.WouldSortingChangeDiffsAsync(context.Config);

        Assert.True(result);
    }

    [Fact]
    public async Task WouldSortingChangeDiffsAsync_ReturnsFalse_WhenRealChangesRemain()
    {
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*a.esm", "*b.esm");
        await context.WritePluginsAsync("*a.esm", "*c.esm");

        bool result = await FileService.WouldSortingChangeDiffsAsync(context.Config);

        Assert.False(result);
    }
}
