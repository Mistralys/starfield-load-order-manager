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
}
