using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LoadOrderKeeper.Models;
using LoadOrderKeeper.Services;
using LoadOrderKeeper.Tests.Fixtures;
using Xunit;

namespace LoadOrderKeeper.Tests.Services;

[Collection(LocaleSequentialCollection.Name)]
public sealed class DebugStateServiceTests : IClassFixture<EnglishLocaleFixture>
{
    public DebugStateServiceTests(EnglishLocaleFixture localeFixture)
    {
        _ = localeFixture; // Ensures en-US locale is active for all tests in this class
    }

    private static DiffLineModel Line(DiffChangeType type, string name, int? refNumber = null, int? curNumber = null) =>
        new(name, name, type, refNumber, curNumber);

    [Fact]
    public async Task CaptureDebugStateAsync_DropsContextAndSeparators_AndWritesChangeTypeNames()
    {
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*a.esm", "*b.esm");
        await context.WritePluginsAsync("*a.esm", "*c.esm");

        var replaced = Line(DiffChangeType.Replaced, "b.esm", 2, 2);
        replaced.DependentChanges.Add(Line(DiffChangeType.Moved, "d.esm", 3, 4));
        var diff = new[]
        {
            Line(DiffChangeType.Unchanged, "a.esm", 1, 1),
            replaced,
            Line(DiffChangeType.Separator, string.Empty),
            Line(DiffChangeType.Added, "e.esm", null, 9),
        };

        string json = await DebugStateService.CaptureDebugStateAsync(context.Config, diff);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal(2, root.GetProperty("TotalChangesDetected").GetInt32());
        Assert.Equal(1, root.GetProperty("TotalDependentChanges").GetInt32());

        var changes = root.GetProperty("ChangeList");
        Assert.Equal(new[] { "Replaced", "Added" }, changes.EnumerateArray().Select(c => c.GetProperty("ChangeType").GetString()).ToArray());
        Assert.Equal("Moved", changes[0].GetProperty("DependentChanges")[0].GetProperty("ChangeType").GetString());
        Assert.DoesNotContain("Unchanged", changes.GetRawText());
        Assert.DoesNotContain("Separator", changes.GetRawText());
    }

    [Fact]
    public async Task CaptureDebugStateAsync_IncludesEnvironmentProfilesAndFileFacts()
    {
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*a.esm");
        await context.WritePluginsAsync("*a.esm");

        string json = await DebugStateService.CaptureDebugStateAsync(context.Config, Array.Empty<DiffLineModel>());

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("Environment").GetProperty("OperatingSystem").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("Environment").GetProperty("ActiveCulture").GetString()));

        var activeProfile = root.GetProperty("Profiles").EnumerateArray().Single(p => p.GetProperty("IsActive").GetBoolean());
        Assert.Equal("default", activeProfile.GetProperty("Id").GetString());

        Assert.True(root.GetProperty("PluginsTxt").GetProperty("Exists").GetBoolean());
        Assert.True(root.GetProperty("Reference").GetProperty("IsValidUtf8").GetBoolean());
        Assert.Empty(root.GetProperty("CaptureWarnings").EnumerateArray());
    }

    [Theory]
    [InlineData(new byte[0], true, false, "None")]
    [InlineData(new byte[] { 0x61, 0x0D, 0x0A, 0x62, 0x0D, 0x0A }, true, false, "CRLF")]
    [InlineData(new byte[] { 0x61, 0x0A, 0x62, 0x0A }, true, false, "LF")]
    [InlineData(new byte[] { 0x61, 0x0D, 0x0A, 0x62, 0x0A }, true, false, "Mixed")]
    [InlineData(new byte[] { 0xEF, 0xBB, 0xBF, 0x61, 0x0A }, true, true, "LF")]
    [InlineData(new byte[] { 0x61, 0xFF, 0xFE, 0x80, 0x0A }, false, false, "LF")]
    public async Task ReadFileStateAsync_DetectsEncodingBomAndLineEndings(byte[] content, bool validUtf8, bool bom, string lineEndings)
    {
        using var context = new TestConfigContext();
        await File.WriteAllBytesAsync(context.PluginsFilePath, content);

        var state = await DebugStateService.ReadFileStateAsync(context.PluginsFilePath);

        Assert.True(state.Exists);
        Assert.Equal(content.Length, state.SizeBytes);
        Assert.Equal(validUtf8, state.IsValidUtf8);
        Assert.Equal(bom, state.HasUtf8Bom);
        Assert.Equal(lineEndings, state.LineEndings);
    }

    [Fact]
    public async Task ReadFileStateAsync_MissingFile_ReportsNotExisting()
    {
        var state = await DebugStateService.ReadFileStateAsync(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));

        Assert.False(state.Exists);
    }

    [Fact]
    public void SanitizeText_ReplacesUserProfileInsideFreeText_CaseInsensitively()
    {
        string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        string result = DebugStateService.SanitizeText($"Failed to read {profile.ToUpperInvariant()}\\AppData\\Plugins.txt");

        Assert.Equal("Failed to read %USERPROFILE%\\AppData\\Plugins.txt", result);
    }
}
