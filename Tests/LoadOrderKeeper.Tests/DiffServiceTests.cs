using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LoadOrderKeeper.Models;
using LoadOrderKeeper.Services;
using LoadOrderKeeper.Tests.Fixtures;
using Xunit;

namespace LoadOrderKeeper.Tests;

[Collection(LocaleSequentialCollection.Name)]
public class DiffServiceTests : IClassFixture<EnglishLocaleFixture>
{
    public DiffServiceTests(EnglishLocaleFixture localeFixture)
    {
        _ = localeFixture; // Ensures en-US culture is active for the lifetime of this test class
    }

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

        var changed = diff.Where(d => d.ChangeType != DiffChangeType.Unchanged && d.ChangeType != DiffChangeType.Separator).ToList();
        var entry = Assert.Single(changed);
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

        var changed = diff.Where(d => d.ChangeType != DiffChangeType.Unchanged && d.ChangeType != DiffChangeType.Separator).ToList();
        Assert.Collection(
            changed,
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

        // Verify only c appears as dependent (under inserted), not as a top-level changed item
        Assert.DoesNotContain(diff, item => item.FileName == "c.esm" && item.ChangeType != DiffChangeType.Unchanged && item.ChangeType != DiffChangeType.Separator);
        
        // e, f, g should not appear as changed items since they didn't move
        // (they may appear as Unchanged context lines)
        Assert.DoesNotContain(diff, item => item.FileName == "e.esm" && item.ChangeType != DiffChangeType.Unchanged && item.ChangeType != DiffChangeType.Separator);
        Assert.DoesNotContain(diff, item => item.FileName == "f.esm" && item.ChangeType != DiffChangeType.Unchanged && item.ChangeType != DiffChangeType.Separator);
        Assert.DoesNotContain(diff, item => item.FileName == "g.esm" && item.ChangeType != DiffChangeType.Unchanged && item.ChangeType != DiffChangeType.Separator);

        // Primary change count should be 2 (inserted + removed)
        Assert.Equal(2, diff.Count(d => d.ChangeType != DiffChangeType.Unchanged && d.ChangeType != DiffChangeType.Separator));
    }

    [Fact]
    public async Task GetPluginsDiffAsync_DetectsAddedModsAfterRemovals()
    {
        // Scenario: Remove mods from the end, then add MORE new mods at the very end
        // Reference: a(1), b(2), c(3), d(4), e(5)
        // Current: a(1), b(2), c(3), new1(4), new2(5), new3(6)
        //
        // Changes:
        // - d removed from position 4
        // - e removed from position 5  
        // - new1 replaces d at position 4
        // - new2 replaces e at position 5
        // - new3 added at position 6
        //
        // Only new3 should be "Added" since it's after all reference mods
        using var context = new TestConfigContext();
        
        await context.WriteReferenceAsync(
            "*a.esm",    // 1
            "*b.esm",    // 2
            "*c.esm",    // 3
            "*d.esm",    // 4 - will be removed
            "*e.esm"     // 5 - will be removed
        );
        
        await context.WritePluginsAsync(
            "*a.esm",     // 1
            "*b.esm",     // 2
            "*c.esm",     // 3
            "*new1.esm",  // 4 - replaces d
            "*new2.esm",  // 5 - replaces e
            "*new3.esm"   // 6 - should be "Added", not "Inserted"
        );

        var diff = await DiffService.GetPluginsDiffAsync(context.Config);

        // Verify new3 is classified as Added
        var new3 = diff.FirstOrDefault(item => item.FileName == "new3.esm");
        Assert.NotNull(new3);
        Assert.Equal(DiffChangeType.Added, new3.ChangeType);
        Assert.Equal(6, new3.CurrentNumber);

        // Verify replacements
        var replaced1 = diff.FirstOrDefault(item => item.FileName == "d.esm");
        Assert.NotNull(replaced1);
        Assert.Equal(DiffChangeType.Replaced, replaced1.ChangeType);

        var replaced2 = diff.FirstOrDefault(item => item.FileName == "e.esm");
        Assert.NotNull(replaced2);
        Assert.Equal(DiffChangeType.Replaced, replaced2.ChangeType);
    }

    [Fact]
    public async Task GetPluginsDiffAsync_DetectsAddedModWhenRemovalAtEnd()
    {
        // User's exact scenario:
        // - Remove "Easy Digipick" from position 20 (middle of list)
        // - Add "newmod" at the end (position 98)
        // - All other 97 mods stay at their positions
        //
        // Expected: newmod should be classified as "Added", not "Inserted"
        // because it's after all EXISTING reference mods
        using var context = new TestConfigContext();
        
        await context.WriteReferenceAsync(
            "*a.esm",         // 1
            "*b.esm",         // 2
            "*c.esm",         // 3
            "*toremove.esm",  // 4 - will be removed
            "*d.esm",         // 5
            "*e.esm"          // 6
        );
        
        await context.WritePluginsAsync(
            "*a.esm",       // 1
            "*b.esm",       // 2
            "*c.esm",       // 3
            "*d.esm",       // 4 - moved up due to removal
            "*e.esm",       // 5 - moved up due to removal
            "*newmod.esm"   // 6 - should be "Added", not "Inserted"
        );

        var diff = await DiffService.GetPluginsDiffAsync(context.Config);

        // Verify newmod is classified as Added
        var newMod = diff.FirstOrDefault(item => item.FileName == "newmod.esm");
        Assert.NotNull(newMod);
        Assert.Equal(DiffChangeType.Added, newMod.ChangeType);
        Assert.Equal(6, newMod.CurrentNumber);

        // Verify toremove was removed
        var removed = diff.FirstOrDefault(item => item.FileName == "toremove.esm");
        Assert.NotNull(removed);
        Assert.Equal(DiffChangeType.Removed, removed.ChangeType);
        Assert.Equal(4, removed.ReferenceNumber);
    }

    // -------------------------------------------------------------------------
    // LCS unit tests
    // -------------------------------------------------------------------------

    [Fact]
    public void ComputeLcs_ReturnsCorrectLcs_ForIdenticalLists()
    {
        // Identical lists: every element is part of the LCS, paired at matching indices.
        var list = new List<string> { "a.esm", "b.esm", "c.esm" };

        var lcs = DiffService.ComputeLcs(list, list, StringComparer.OrdinalIgnoreCase);

        Assert.Equal(list.Count, lcs.Count);
        for (int i = 0; i < list.Count; i++)
        {
            Assert.Equal(i, lcs[i].refIndex);
            Assert.Equal(i, lcs[i].curIndex);
        }
    }

    [Fact]
    public void ComputeLcs_ReturnsCorrectLcs_ForDisjointLists()
    {
        // No shared elements: the LCS must be empty.
        var reference = new List<string> { "a.esm", "b.esm", "c.esm" };
        var current   = new List<string> { "x.esm", "y.esm", "z.esm" };

        var lcs = DiffService.ComputeLcs(reference, current, StringComparer.OrdinalIgnoreCase);

        Assert.Empty(lcs);
    }

    [Fact]
    public void ComputeLcs_ReturnsCorrectLcs_ForPartialOverlap()
    {
        // Reference: a, b, c, d, e
        // Current:   b, a, d, e, f    (a reordered to pos 2, c removed, f added)
        // LCS should be the longest subsequence in the same relative order in both lists.
        // Valid LCS candidates: "a,d,e" (length 3) or "b,d,e" (length 3).
        // The algorithm picks one deterministically; we just verify length and that all pairs
        // are consistent (reference[ri] == current[ci]).
        var reference = new List<string> { "a.esm", "b.esm", "c.esm", "d.esm", "e.esm" };
        var current   = new List<string> { "b.esm", "a.esm", "d.esm", "e.esm", "f.esm" };

        var lcs = DiffService.ComputeLcs(reference, current, StringComparer.OrdinalIgnoreCase);

        // LCS length must be 3 (max possible given the reordering of a/b)
        Assert.Equal(3, lcs.Count);

        // Each pair must actually match in both lists
        foreach (var (ri, ci) in lcs)
        {
            Assert.Equal(reference[ri], current[ci], StringComparer.OrdinalIgnoreCase);
        }

        // Indices must be strictly increasing (valid subsequence)
        for (int i = 1; i < lcs.Count; i++)
        {
            Assert.True(lcs[i].refIndex > lcs[i - 1].refIndex, "refIndex must be strictly increasing");
            Assert.True(lcs[i].curIndex > lcs[i - 1].curIndex, "curIndex must be strictly increasing");
        }
    }

    [Fact]
    public void ComputeLcs_IsCaseInsensitive()
    {
        // Same filenames, different casing — should be treated as equal when using
        // StringComparer.OrdinalIgnoreCase, yielding a full-length LCS.
        var reference = new List<string> { "Alpha.ESM", "Beta.ESM" };
        var current   = new List<string> { "alpha.esm", "beta.esm" };

        var lcs = DiffService.ComputeLcs(reference, current, StringComparer.OrdinalIgnoreCase);

        Assert.Equal(2, lcs.Count);
        Assert.Equal(0, lcs[0].refIndex);
        Assert.Equal(0, lcs[0].curIndex);
        Assert.Equal(1, lcs[1].refIndex);
        Assert.Equal(1, lcs[1].curIndex);
    }

    // -------------------------------------------------------------------------
    // Context line / TrimToContextWindow tests (tested indirectly via GetPluginsDiffAsync)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task TrimToContextWindow_SingleChangeMiddle_ShowsOneNeighborEachSide()
    {
        // One change in the middle of a 5-item list (c replaced by X).
        // Expected context: one Unchanged before (b) and one Unchanged after (d).
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*a.esm", "*b.esm", "*c.esm", "*d.esm", "*e.esm");
        await context.WritePluginsAsync("*a.esm", "*b.esm", "*x.esm", "*d.esm", "*e.esm");

        var diff = await DiffService.GetPluginsDiffAsync(context.Config);

        var changed = diff.Where(d => d.ChangeType != DiffChangeType.Unchanged && d.ChangeType != DiffChangeType.Separator).ToList();
        Assert.Single(changed);

        // One context line above the change (b) and one below (d)
        var unchangedItems = diff.Where(d => d.ChangeType == DiffChangeType.Unchanged).Select(d => d.FileName).ToList();
        Assert.Contains("b.esm", unchangedItems);
        Assert.Contains("d.esm", unchangedItems);

        // Items far from the change (a, e) should not appear
        Assert.DoesNotContain("a.esm", unchangedItems);
        Assert.DoesNotContain("e.esm", unchangedItems);
    }

    [Fact]
    public async Task TrimToContextWindow_AdjacentChanges_NoSeparatorBetween()
    {
        // Two adjacent changes (b and c replaced). The context lines around them
        // share neighbors, so no separator should be inserted between the groups.
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*a.esm", "*b.esm", "*c.esm", "*d.esm", "*e.esm");
        await context.WritePluginsAsync("*a.esm", "*x.esm", "*y.esm", "*d.esm", "*e.esm");

        var diff = await DiffService.GetPluginsDiffAsync(context.Config);

        Assert.DoesNotContain(diff, item => item.ChangeType == DiffChangeType.Separator);
    }

    [Fact]
    public async Task TrimToContextWindow_DistantChanges_InsertsSeparator()
    {
        // Two changes separated by many unchanged items.
        // A separator should appear between the two context groups.
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync(
            "*a.esm", "*b.esm", "*x.esm", "*c.esm",
            "*d.esm", "*e.esm", "*f.esm", "*y.esm",
            "*g.esm", "*h.esm");
        await context.WritePluginsAsync(
            "*a.esm", "*b.esm", "*p.esm", "*c.esm",
            "*d.esm", "*e.esm", "*f.esm", "*q.esm",
            "*g.esm", "*h.esm");

        var diff = await DiffService.GetPluginsDiffAsync(context.Config);

        Assert.Contains(diff, item => item.ChangeType == DiffChangeType.Separator);
    }

    [Fact]
    public async Task TrimToContextWindow_ChangeAtStart_NoContextAbove()
    {
        // Changed item is the first mod in the list. No context line above it.
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*a.esm", "*b.esm", "*c.esm");
        await context.WritePluginsAsync("*x.esm", "*b.esm", "*c.esm");

        var diff = await DiffService.GetPluginsDiffAsync(context.Config);

        var changed = diff.Where(d => d.ChangeType != DiffChangeType.Unchanged && d.ChangeType != DiffChangeType.Separator).ToList();
        Assert.Single(changed);

        // b.esm is the context line after the change
        Assert.Contains(diff, item => item.FileName == "b.esm" && item.ChangeType == DiffChangeType.Unchanged);

        // No context above the first item
        var unchangedItems = diff.Where(d => d.ChangeType == DiffChangeType.Unchanged).Select(d => d.FileName).ToList();
        Assert.DoesNotContain("c.esm", unchangedItems);
    }

    [Fact]
    public async Task TrimToContextWindow_ChangeAtEnd_NoContextBelow()
    {
        // Changed item is the last mod in the list. No context line below it.
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*a.esm", "*b.esm", "*c.esm");
        await context.WritePluginsAsync("*a.esm", "*b.esm", "*x.esm");

        var diff = await DiffService.GetPluginsDiffAsync(context.Config);

        var changed = diff.Where(d => d.ChangeType != DiffChangeType.Unchanged && d.ChangeType != DiffChangeType.Separator).ToList();
        Assert.Single(changed);

        // b.esm is the context line before the change
        Assert.Contains(diff, item => item.FileName == "b.esm" && item.ChangeType == DiffChangeType.Unchanged);

        // No context below the last item
        var unchangedItems = diff.Where(d => d.ChangeType == DiffChangeType.Unchanged).Select(d => d.FileName).ToList();
        Assert.DoesNotContain("a.esm", unchangedItems);
    }

    // -------------------------------------------------------------------------
    // Dependent-change causal text tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DependentChanges_HaveCauseFileName()
    {
        // When a mod is removed and causes dependent shifts, the parent entry
        // should have DependentChangeCauseFileName set to the removed mod's filename.
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*a.esm", "*b.esm", "*c.esm", "*d.esm");
        await context.WritePluginsAsync("*a.esm", "*c.esm", "*d.esm");

        var diff = await DiffService.GetPluginsDiffAsync(context.Config);

        var removed = diff.FirstOrDefault(item => item.FileName == "b.esm");
        Assert.NotNull(removed);
        Assert.True(removed.HasDependentChanges);
        Assert.Equal("b.esm", removed.DependentChangeCauseFileName);
    }

    [Fact]
    public async Task DependentChanges_HaveCauseAction()
    {
        // When a mod is removed and causes dependent shifts, the parent entry
        // should have DependentChangeCauseAction set to "DependentCause_Removed".
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*a.esm", "*b.esm", "*c.esm", "*d.esm");
        await context.WritePluginsAsync("*a.esm", "*c.esm", "*d.esm");

        var diff = await DiffService.GetPluginsDiffAsync(context.Config);

        var removed = diff.FirstOrDefault(item => item.FileName == "b.esm");
        Assert.NotNull(removed);
        Assert.True(removed.HasDependentChanges);
        Assert.Equal("DependentCause_Removed", removed.DependentChangeCauseAction);
    }

    [Fact]
    public async Task DependentChanges_InsertedMod_HaveCauseAction()
    {
        // When a mod is inserted and causes dependent shifts, the parent entry
        // should have DependentChangeCauseAction set to "DependentCause_Inserted".
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*a.esm", "*b.esm", "*c.esm");
        await context.WritePluginsAsync("*a.esm", "*new.esm", "*b.esm", "*c.esm");

        var diff = await DiffService.GetPluginsDiffAsync(context.Config);

        var inserted = diff.FirstOrDefault(item => item.FileName == "new.esm");
        Assert.NotNull(inserted);
        Assert.True(inserted.HasDependentChanges);
        Assert.Equal("DependentCause_Inserted", inserted.DependentChangeCauseAction);
    }

    [Fact]
    public async Task DependentChanges_SummaryIsNotEmpty_WhenCauseSet()
    {
        // Entries with dependent changes and a cause should have a non-empty DependentChangesSummary.
        using var context = new TestConfigContext();
        await context.WriteReferenceAsync("*a.esm", "*b.esm", "*c.esm", "*d.esm");
        await context.WritePluginsAsync("*a.esm", "*c.esm", "*d.esm");

        var diff = await DiffService.GetPluginsDiffAsync(context.Config);

        var removed = diff.FirstOrDefault(item => item.FileName == "b.esm");
        Assert.NotNull(removed);
        Assert.True(removed.HasDependentChanges);
        Assert.NotEmpty(removed.DependentChangesSummary);
        Assert.Contains("b.esm", removed.DependentChangesSummary);
    }
}
