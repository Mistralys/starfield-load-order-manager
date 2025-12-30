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
    public async Task HasDeletedModsAsync_ReturnsTrue_WhenReferenceEntryMissing()
    {
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*only.esm");
        await context.WritePluginsAsync();

        bool hasDeleted = await FileService.HasDeletedModsAsync(context.Config);

        Assert.True(hasDeleted);
    }
}
