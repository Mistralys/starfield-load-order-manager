using System.Linq;
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
        Assert.Contains(diff, item => item.ChangeType == DiffChangeType.Inserted && item.FileName == "d.esm");
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

    [Fact]
    public async Task GetPluginsDiffAsync_DetectsInsertedMods()
    {
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*a.esm", "*b.esm", "*c.esm");
        await context.WritePluginsAsync("*a.esm", "*new.esm", "*b.esm", "*c.esm");

        var diff = await DiffService.GetPluginsDiffAsync(context.Config);

        var inserted = diff.FirstOrDefault(item => item.FileName == "new.esm");
        Assert.NotNull(inserted);
        Assert.Equal(DiffChangeType.Inserted, inserted.ChangeType);
        Assert.Equal(2, inserted.CurrentNumber);
    }

    [Fact]
    public async Task GetPluginsDiffAsync_DetectsAddedModsAtEnd()
    {
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*a.esm", "*b.esm");
        await context.WritePluginsAsync("*a.esm", "*b.esm", "*new.esm");

        var diff = await DiffService.GetPluginsDiffAsync(context.Config);

        var added = diff.FirstOrDefault(item => item.FileName == "new.esm");
        Assert.NotNull(added);
        Assert.Equal(DiffChangeType.Added, added.ChangeType);
        Assert.Equal(3, added.CurrentNumber);
    }

    [Fact]
    public async Task GetPluginsDiffAsync_DetectsDependentChangesForRemovedMod()
    {
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*a.esm", "*b.esm", "*c.esm", "*d.esm");
        await context.WritePluginsAsync("*a.esm", "*c.esm", "*d.esm");

        var diff = await DiffService.GetPluginsDiffAsync(context.Config);

        var removed = diff.FirstOrDefault(item => item.FileName == "b.esm");
        Assert.NotNull(removed);
        Assert.Equal(DiffChangeType.Removed, removed.ChangeType);
        Assert.True(removed.HasDependentChanges);
        Assert.Equal(2, removed.DependentChanges.Count);
        Assert.All(removed.DependentChanges, dep => Assert.Equal(DiffChangeType.Moved, dep.ChangeType));
    }

    [Fact]
    public async Task GetPluginsDiffAsync_DetectsDependentChangesForInsertedMod()
    {
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*a.esm", "*b.esm", "*c.esm");
        await context.WritePluginsAsync("*a.esm", "*new.esm", "*b.esm", "*c.esm");

        var diff = await DiffService.GetPluginsDiffAsync(context.Config);

        var inserted = diff.FirstOrDefault(item => item.FileName == "new.esm");
        Assert.NotNull(inserted);
        Assert.Equal(DiffChangeType.Inserted, inserted.ChangeType);
        Assert.True(inserted.HasDependentChanges);
        Assert.Equal(2, inserted.DependentChanges.Count);
        Assert.All(inserted.DependentChanges, dep => Assert.Equal(DiffChangeType.Moved, dep.ChangeType));
    }

    [Fact]
    public async Task GetPluginsDiffAsync_NoDependentChangesWhenNoMovedMods()
    {
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*a.esm", "*b.esm");
        await context.WritePluginsAsync("*a.esm", "*new.esm");

        var diff = await DiffService.GetPluginsDiffAsync(context.Config);

        var replaced = diff.FirstOrDefault(item => item.FileName == "b.esm");
        Assert.NotNull(replaced);
        Assert.Equal(DiffChangeType.Replaced, replaced.ChangeType);
    }

    [Fact]
    public async Task GetPluginsDiffAsync_StopsDependentDetectionAtNonMovedChange()
    {
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*a.esm", "*b.esm", "*c.esm", "*d.esm");
        await context.WritePluginsAsync("*a.esm", "*c.esm", "*new.esm", "*d.esm");

        var diff = await DiffService.GetPluginsDiffAsync(context.Config);

        var removed = diff.FirstOrDefault(item => item.FileName == "b.esm");
        Assert.NotNull(removed);
        Assert.True(removed.HasDependentChanges);
        Assert.Single(removed.DependentChanges);
        Assert.Equal("c.esm", removed.DependentChanges[0].FileName);
    }

    [Fact]
    public async Task GetPluginsDiffAsync_HandlesInsertedAndRemovedModsCorrectly()
    {
        // Scenario: Insert mod between b and c, remove mod d
        // Reference: a(1), b(2), c(3), d(4), e(5), f(6), g(7)
        // Current: a(1), b(2), new(3), c(4), e(5), f(6), g(7)
        //
        // Changes:
        // - new is inserted at position 3
        // - c moves from 3 to 4 (pushed down by new)
        // - d is removed from position 4
        // - e, f, g stay at positions 5, 6, 7 (removal pulls them up, insertion pushes them down = net zero)
        //
        // Expected dependents:
        // - inserted "new": should have c as dependent (c was pushed down)
        // - removed "d": should have NO dependents (no mods actually moved due to its removal alone)
        using var context = new TestConfigContext();
        
        await context.WriteReferenceAsync(
            "*a.esm",    // 1
            "*b.esm",    // 2
            "*c.esm",    // 3
            "*d.esm",    // 4 - will be removed
            "*e.esm",    // 5
            "*f.esm",    // 6
            "*g.esm"     // 7
        );
        
        await context.WritePluginsAsync(
            "*a.esm",    // 1 - unchanged
            "*b.esm",    // 2 - unchanged
            "*new.esm",  // 3 - inserted
            "*c.esm",    // 4 - moved from 3
            "*e.esm",    // 5 - unchanged (net effect)
            "*f.esm",    // 6 - unchanged
            "*g.esm"     // 7 - unchanged
        );

        var diff = await DiffService.GetPluginsDiffAsync(context.Config);

        // Verify the inserted mod
        var inserted = diff.FirstOrDefault(item => item.FileName == "new.esm");
        Assert.NotNull(inserted);
        Assert.Equal(DiffChangeType.Inserted, inserted.ChangeType);
        Assert.Equal(3, inserted.CurrentNumber);
        Assert.True(inserted.HasDependentChanges, "Inserted mod should have dependent changes");
        
        // The inserted mod should capture c.esm as dependent (moved from 3 to 4)
        Assert.Single(inserted.DependentChanges);
        Assert.Equal("c.esm", inserted.DependentChanges[0].FileName);

        // Verify the removed mod
        var removed = diff.FirstOrDefault(item => item.FileName == "d.esm");
        Assert.NotNull(removed);
        Assert.Equal(DiffChangeType.Removed, removed.ChangeType);
        Assert.Equal(4, removed.ReferenceNumber);
        
        // The removed mod should have NO dependents because e, f, g don't actually move
        // (they get pulled up by d removal but pushed down by new insertion = net zero)
        Assert.False(removed.HasDependentChanges, "Removed mod should NOT have dependent changes in this scenario");

        // Verify only c appears as dependent (under inserted)
        Assert.DoesNotContain(diff, item => item.FileName == "c.esm");
        
        // e, f, g should not appear at all since they didn't move
        Assert.DoesNotContain(diff, item => item.FileName == "e.esm");
        Assert.DoesNotContain(diff, item => item.FileName == "f.esm");
        Assert.DoesNotContain(diff, item => item.FileName == "g.esm");

        // Total count should be 2 (inserted + removed)
        Assert.Equal(2, diff.Count);
    }
}
