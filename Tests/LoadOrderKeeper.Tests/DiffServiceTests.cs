using System.Linq;
using System.Threading.Tasks;
using LoadOrderKeeper.Models;
using LoadOrderKeeper.Services;
using Xunit;

namespace LoadOrderKeeper.Tests;

public class DiffServiceTests
{
    [Fact]
    public async Task GetPluginsDiffAsync_ReturnsExpectedChangeTypes()
    {
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*a.esm", "*b.esm");
        await context.WritePluginsAsync("*a.esm", "*c.esm");

        var diff = await DiffService.GetPluginsDiffAsync(context.Config);

        Assert.Collection(
            diff,
            item =>
            {
                Assert.Equal("*a.esm", item.Text);
                Assert.Equal(DiffChangeType.Unchanged, item.ChangeType);
            },
            item =>
            {
                Assert.Equal("*b.esm", item.Text);
                Assert.Equal(DiffChangeType.Removed, item.ChangeType);
            },
            item =>
            {
                Assert.Equal("*c.esm", item.Text);
                Assert.Equal(DiffChangeType.Added, item.ChangeType);
            });
    }

    [Fact]
    public async Task GetPluginsDiffAsync_ReturnsEmpty_WhenFilesIdentical()
    {
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*x.esm", "*y.esm");
        await context.WritePluginsAsync("*x.esm", "*y.esm");

        var diff = await DiffService.GetPluginsDiffAsync(context.Config);

        Assert.Empty(diff.Where(d => d.ChangeType != DiffChangeType.Unchanged));
    }
}
