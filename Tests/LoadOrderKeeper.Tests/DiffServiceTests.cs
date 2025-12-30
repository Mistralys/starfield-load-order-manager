using System.Threading.Tasks;
using LoadOrderKeeper.Models;
using LoadOrderKeeper.Services;
using Xunit;

namespace LoadOrderKeeper.Tests;

public class DiffServiceTests
{
    [Fact]
    public async Task GetPluginsDiffAsync_ReportsUnpairedAddedAndRemovedMods()
    {
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*a.esm", "*b.esm", "*c.esm");
        await context.WritePluginsAsync("*d.esm", "*a.esm", "*c.esm");

        var diff = await DiffService.GetPluginsDiffAsync(context.Config);

        Assert.Contains(diff, item => item.ChangeType == DiffChangeType.Removed && item.FileName == "b.esm");
        Assert.Contains(diff, item => item.ChangeType == DiffChangeType.Added && item.FileName == "d.esm");
    }

    [Fact]
    public async Task GetPluginsDiffAsync_DetectsReplacements()
    {
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*a.esm", "*b.esm");
        await context.WritePluginsAsync("*a.esm", "*c.esm");

        var diff = await DiffService.GetPluginsDiffAsync(context.Config);

        var entry = Assert.Single(diff);
        Assert.Equal(DiffChangeType.Replaced, entry.ChangeType);
        Assert.Contains("b.esm", entry.Text);
        Assert.Contains("c.esm", entry.Text);
        Assert.Contains("line 2", entry.Text);
    }

    [Fact]
    public async Task GetPluginsDiffAsync_ReturnsEmpty_WhenFilesIdentical()
    {
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*x.esm", "*y.esm");
        await context.WritePluginsAsync("*x.esm", "*y.esm");

        var diff = await DiffService.GetPluginsDiffAsync(context.Config);

        Assert.Empty(diff);
    }

    [Fact]
    public async Task GetPluginsDiffAsync_TreatsDisabledModsAsRemoved()
    {
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*a.esm");
        await context.WritePluginsAsync("a.esm");

        var diff = await DiffService.GetPluginsDiffAsync(context.Config);

        Assert.Single(diff);
        Assert.Equal(DiffChangeType.Removed, diff[0].ChangeType);
        Assert.Contains("a.esm", diff[0].Text);
    }

    [Fact]
    public async Task GetPluginsDiffAsync_ReportsMovedModsForOrderChanges()
    {
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*a.esm", "*b.esm", "*c.esm");
        await context.WritePluginsAsync("*b.esm", "*a.esm", "*c.esm");

        var diff = await DiffService.GetPluginsDiffAsync(context.Config);

        Assert.Collection(
            diff,
            item =>
            {
                Assert.Equal(DiffChangeType.Moved, item.ChangeType);
                Assert.Contains("a.esm", item.Text);
                Assert.Contains("#1", item.Text);
                Assert.Contains("#2", item.Text);
            },
            item =>
            {
                Assert.Equal(DiffChangeType.Moved, item.ChangeType);
                Assert.Contains("b.esm", item.Text);
                Assert.Contains("#2", item.Text);
                Assert.Contains("#1", item.Text);
            });
    }
}
