using System.Threading.Tasks;
using LoadOrderKeeper.Models;
using LoadOrderKeeper.Services;
using Xunit;

namespace LoadOrderKeeper.Tests;

public class DiffServiceTests
{
    [Fact]
    public async Task GetPluginsDiffAsync_ReportsAddedAndRemovedMods()
    {
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*a.esm", "*b.esm");
        await context.WritePluginsAsync("*a.esm", "*c.esm");

        var diff = await DiffService.GetPluginsDiffAsync(context.Config);

        Assert.Collection(
            diff,
            item =>
            {
                Assert.Equal(DiffChangeType.Removed, item.ChangeType);
                Assert.Contains("*b.esm", item.Text);
                Assert.Contains("#2", item.Text);
            },
            item =>
            {
                Assert.Equal(DiffChangeType.Added, item.ChangeType);
                Assert.Contains("*c.esm", item.Text);
                Assert.Contains("#2", item.Text);
            });
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
        Assert.Contains("*a.esm", diff[0].Text);
    }
}
