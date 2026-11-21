using System;
using System.Collections.Generic;
using System.Linq;
using LoadOrderKeeper.Models;
using LoadOrderKeeper.Services;
using LoadOrderKeeper.Tests.Fixtures;
using Xunit;

namespace LoadOrderKeeper.Tests;

/// <summary>
/// Isolation tests for <see cref="DiffService.ClassifyChanges"/>. Each test constructs
/// <see cref="ModEntryModel"/> lists directly (no file I/O), computes LCS via
/// <see cref="DiffService.ComputeLcs"/>, and calls <see cref="DiffService.ClassifyChanges"/>
/// directly.
/// </summary>
/// <remarks>
/// All assertions target structural properties only — <c>ChangeType</c>, <c>FileName</c>,
/// <c>ReferenceNumber</c>, <c>CurrentNumber</c>, <c>DependentChanges</c> — never localized
/// <c>.Text</c> content. The <see cref="EnglishLocaleFixture"/> is required because
/// <c>ClassifyChanges</c> calls <c>_localization.GetString()</c> internally to populate
/// <c>DiffLineModel.Text</c>, but no assertions here depend on those strings.
/// </remarks>
[Collection(LocaleSequentialCollection.Name)]
public sealed class ClassifyChangesTests : IClassFixture<EnglishLocaleFixture>
{
    public ClassifyChangesTests(EnglishLocaleFixture localeFixture)
    {
        _ = localeFixture; // Ensures LocalizationService singleton is initialized for the lifetime of this class
    }

    // ------------------------------------------------------------------
    // Helper: build a ModEntryModel with FileName and LineNumber set
    // ------------------------------------------------------------------

    private static ModEntryModel Mod(string fileName, int lineNumber)
        => new ModEntryModel($"*{fileName}", lineNumber);

    // ------------------------------------------------------------------
    // Helper: compute LCS from two ModEntryModel lists
    // ------------------------------------------------------------------

    private static List<(int refIndex, int curIndex)> Lcs(
        IReadOnlyList<ModEntryModel> reference,
        IReadOnlyList<ModEntryModel> current)
    {
        var refNames = reference.Select(m => m.FileName).ToList();
        var curNames = current.Select(m => m.FileName).ToList();
        return DiffService.ComputeLcs(refNames, curNames, StringComparer.OrdinalIgnoreCase);
    }

    // ==================================================================
    // Step 9a — Unchanged list returns empty result
    // ==================================================================

    [Fact]
    public void ClassifyChanges_UnchangedList_ReturnsEmpty()
    {
        var reference = new[]
        {
            Mod("a.esm", 1),
            Mod("b.esm", 2),
            Mod("c.esm", 3),
        };
        var current = new[]
        {
            Mod("a.esm", 1),
            Mod("b.esm", 2),
            Mod("c.esm", 3),
        };

        var lcs    = Lcs(reference, current);
        var result = DiffService.ClassifyChanges(reference, current, lcs);

        Assert.Empty(result);
    }

    // ==================================================================
    // Step 9b — Single removal with dependent Moved
    // ==================================================================

    [Fact]
    public void ClassifyChanges_SingleRemoval_ReturnsRemovedWithDependentMoved()
    {
        // Reference: a(1) b(2) c(3)
        // Current:   a(1) c(2)           — b removed, c shifts up
        var reference = new[]
        {
            Mod("a.esm", 1),
            Mod("b.esm", 2),
            Mod("c.esm", 3),
        };
        var current = new[]
        {
            Mod("a.esm", 1),
            Mod("c.esm", 2),
        };

        var lcs    = Lcs(reference, current);
        var result = DiffService.ClassifyChanges(reference, current, lcs);

        var changed = result.Where(d => d.ChangeType != DiffChangeType.Unchanged && d.ChangeType != DiffChangeType.Separator).ToList();
        var removed = Assert.Single(changed);
        Assert.Equal(DiffChangeType.Removed, removed.ChangeType);
        Assert.Equal("b.esm", removed.FileName);
        Assert.Equal(2, removed.ReferenceNumber);

        var dependent = Assert.Single(removed.DependentChanges);
        Assert.Equal(DiffChangeType.Moved, dependent.ChangeType);
        Assert.Equal("c.esm", dependent.FileName);
        Assert.Equal(3, dependent.ReferenceNumber);
        Assert.Equal(2, dependent.CurrentNumber);
    }

    // ==================================================================
    // Step 9c — Single insertion with dependent Moved entries
    // ==================================================================

    [Fact]
    public void ClassifyChanges_SingleInsertion_ReturnsInsertedWithDependentMoved()
    {
        // Reference: a(1) b(2) c(3)
        // Current:   a(1) new(2) b(3) c(4)   — new inserted, b and c shift down
        var reference = new[]
        {
            Mod("a.esm", 1),
            Mod("b.esm", 2),
            Mod("c.esm", 3),
        };
        var current = new[]
        {
            Mod("a.esm",   1),
            Mod("new.esm", 2),
            Mod("b.esm",   3),
            Mod("c.esm",   4),
        };

        var lcs    = Lcs(reference, current);
        var result = DiffService.ClassifyChanges(reference, current, lcs);

        var changed = result.Where(d => d.ChangeType != DiffChangeType.Unchanged && d.ChangeType != DiffChangeType.Separator).ToList();
        var inserted = Assert.Single(changed);
        Assert.Equal(DiffChangeType.Inserted, inserted.ChangeType);
        Assert.Equal("new.esm", inserted.FileName);
        Assert.Equal(2, inserted.CurrentNumber);

        Assert.Equal(2, inserted.DependentChanges.Count);
        Assert.All(inserted.DependentChanges, dep => Assert.Equal(DiffChangeType.Moved, dep.ChangeType));
        Assert.Contains(inserted.DependentChanges, dep => dep.FileName == "b.esm");
        Assert.Contains(inserted.DependentChanges, dep => dep.FileName == "c.esm");
    }

    // ==================================================================
    // Step 9d — Single addition at end
    // ==================================================================

    [Fact]
    public void ClassifyChanges_AdditionAtEnd_ReturnsAddedEntry()
    {
        // Reference: a(1) b(2)
        // Current:   a(1) b(2) new(3)   — new appended beyond all reference mods
        var reference = new[]
        {
            Mod("a.esm", 1),
            Mod("b.esm", 2),
        };
        var current = new[]
        {
            Mod("a.esm",   1),
            Mod("b.esm",   2),
            Mod("new.esm", 3),
        };

        var lcs    = Lcs(reference, current);
        var result = DiffService.ClassifyChanges(reference, current, lcs);

        var changed = result.Where(d => d.ChangeType != DiffChangeType.Unchanged && d.ChangeType != DiffChangeType.Separator).ToList();
        var added = Assert.Single(changed);
        Assert.Equal(DiffChangeType.Added, added.ChangeType);
        Assert.Equal("new.esm", added.FileName);
        Assert.Equal(3, added.CurrentNumber);
        Assert.Empty(added.DependentChanges);
    }

    // ==================================================================
    // Step 9e — Replacement at same position
    // ==================================================================

    [Fact]
    public void ClassifyChanges_ReplacementAtSamePosition_ReturnsReplacedEntry()
    {
        // Reference: a(1) b(2)
        // Current:   a(1) c(2)   — b replaced by c at position 2
        var reference = new[]
        {
            Mod("a.esm", 1),
            Mod("b.esm", 2),
        };
        var current = new[]
        {
            Mod("a.esm", 1),
            Mod("c.esm", 2),
        };

        var lcs    = Lcs(reference, current);
        var result = DiffService.ClassifyChanges(reference, current, lcs);

        var changed = result.Where(d => d.ChangeType != DiffChangeType.Unchanged && d.ChangeType != DiffChangeType.Separator).ToList();
        var replaced = Assert.Single(changed);
        Assert.Equal(DiffChangeType.Replaced, replaced.ChangeType);
        Assert.Equal("b.esm", replaced.FileName);
        Assert.Equal("c.esm", replaced.ReplacementFileName);
        Assert.Empty(replaced.DependentChanges);
    }

    // ==================================================================
    // Step 9f — Moved (swapped) mods
    // ==================================================================

    [Fact]
    public void ClassifyChanges_SwappedMods_ReturnsTwoMovedEntries()
    {
        // Reference: a(1) b(2) c(3)
        // Current:   b(1) a(2) c(3)   — a and b swap positions
        var reference = new[]
        {
            Mod("a.esm", 1),
            Mod("b.esm", 2),
            Mod("c.esm", 3),
        };
        var current = new[]
        {
            Mod("b.esm", 1),
            Mod("a.esm", 2),
            Mod("c.esm", 3),
        };

        var lcs    = Lcs(reference, current);
        var result = DiffService.ClassifyChanges(reference, current, lcs);

        var changed = result.Where(d => d.ChangeType != DiffChangeType.Unchanged && d.ChangeType != DiffChangeType.Separator).ToList();
        Assert.Equal(2, changed.Count);
        Assert.All(changed, entry => Assert.Equal(DiffChangeType.Moved, entry.ChangeType));

        var movedA = changed.Single(e => e.FileName == "a.esm");
        Assert.Equal(1, movedA.ReferenceNumber);
        Assert.Equal(2, movedA.CurrentNumber);

        var movedB = changed.Single(e => e.FileName == "b.esm");
        Assert.Equal(2, movedB.ReferenceNumber);
        Assert.Equal(1, movedB.CurrentNumber);
    }

    // ==================================================================
    // Step 9g — Dependent change grouping under removal
    // ==================================================================

    [Fact]
    public void ClassifyChanges_RemovalWithMultipleDependents_GroupsDependentsUnderRemoval()
    {
        // Reference: a(1) b(2) c(3) d(4) e(5)
        // Current:   a(1) c(2) d(3) e(4)   — b removed; c, d, e shift up
        var reference = new[]
        {
            Mod("a.esm", 1),
            Mod("b.esm", 2),
            Mod("c.esm", 3),
            Mod("d.esm", 4),
            Mod("e.esm", 5),
        };
        var current = new[]
        {
            Mod("a.esm", 1),
            Mod("c.esm", 2),
            Mod("d.esm", 3),
            Mod("e.esm", 4),
        };

        var lcs    = Lcs(reference, current);
        var result = DiffService.ClassifyChanges(reference, current, lcs);

        var changed = result.Where(d => d.ChangeType != DiffChangeType.Unchanged && d.ChangeType != DiffChangeType.Separator).ToList();
        var removed = Assert.Single(changed);
        Assert.Equal(DiffChangeType.Removed, removed.ChangeType);
        Assert.Equal("b.esm", removed.FileName);

        Assert.Equal(3, removed.DependentChanges.Count);
        Assert.All(removed.DependentChanges, dep => Assert.Equal(DiffChangeType.Moved, dep.ChangeType));
        Assert.Contains(removed.DependentChanges, dep => dep.FileName == "c.esm");
        Assert.Contains(removed.DependentChanges, dep => dep.FileName == "d.esm");
        Assert.Contains(removed.DependentChanges, dep => dep.FileName == "e.esm");
    }

    // ==================================================================
    // Step 9h — Multiple consecutive replacements (tests cumulativeDeletions stays at 0)
    // ==================================================================

    [Fact]
    public void ClassifyChanges_ConsecutiveReplacements_ReturnsTwoReplacedEntries()
    {
        // Reference: a(1) b(2) c(3) d(4)
        // Current:   a(1) x(2) y(3) d(4)   — b→x and c→y both replaced at same positions
        var reference = new[]
        {
            Mod("a.esm", 1),
            Mod("b.esm", 2),
            Mod("c.esm", 3),
            Mod("d.esm", 4),
        };
        var current = new[]
        {
            Mod("a.esm", 1),
            Mod("x.esm", 2),
            Mod("y.esm", 3),
            Mod("d.esm", 4),
        };

        var lcs    = Lcs(reference, current);
        var result = DiffService.ClassifyChanges(reference, current, lcs);

        var changed = result.Where(d => d.ChangeType != DiffChangeType.Unchanged && d.ChangeType != DiffChangeType.Separator).ToList();
        Assert.Equal(2, changed.Count);
        Assert.All(changed, entry => Assert.Equal(DiffChangeType.Replaced, entry.ChangeType));

        var replacedB = changed.Single(e => e.FileName == "b.esm");
        Assert.Equal("x.esm", replacedB.ReplacementFileName);

        var replacedC = changed.Single(e => e.FileName == "c.esm");
        Assert.Equal("y.esm", replacedC.ReplacementFileName);
    }

    // ==================================================================
    // Step 9i — Replacement with position shift (deletion before replacement)
    // ==================================================================

    [Fact]
    public void ClassifyChanges_DeletionBeforeReplacement_AlignedPositionMatchesCorrectly()
    {
        // Reference: a(1) b(2) c(3) d(4)
        // Current:   a(1) c(2) x(3)
        //   — b is truly removed (shifts subsequent positions by 1)
        //   — d (aligned curPos = 4−1 = 3) is replaced by x
        //   — c shifts from refPos=3 to curPos=2 → dependent of b's removal
        var reference = new[]
        {
            Mod("a.esm", 1),
            Mod("b.esm", 2),
            Mod("c.esm", 3),
            Mod("d.esm", 4),
        };
        var current = new[]
        {
            Mod("a.esm", 1),
            Mod("c.esm", 2),
            Mod("x.esm", 3),
        };

        var lcs    = Lcs(reference, current);
        var result = DiffService.ClassifyChanges(reference, current, lcs);

        // b should be Removed (ref=2)
        var removedB = result.Single(e => e.FileName == "b.esm");
        Assert.Equal(DiffChangeType.Removed, removedB.ChangeType);
        Assert.Equal(2, removedB.ReferenceNumber);

        // d should be Replaced by x (ref=4, aligned cur=4−1=3)
        var replacedD = result.Single(e => e.FileName == "d.esm");
        Assert.Equal(DiffChangeType.Replaced, replacedD.ChangeType);
        Assert.Equal("x.esm", replacedD.ReplacementFileName);
        Assert.Equal(4, replacedD.ReferenceNumber);
        Assert.Equal(3, replacedD.CurrentNumber);
    }

    // ==================================================================
    // Step 9j — Insertion with dependent change attribution
    // ==================================================================

    [Fact]
    public void ClassifyChanges_InsertionWithDependents_AttributesDependentsToInsertion()
    {
        // Reference: a(1) b(2) c(3)
        // Current:   a(1) new(2) b(3) c(4)   — new inserted at 2; b and c shift down
        var reference = new[]
        {
            Mod("a.esm", 1),
            Mod("b.esm", 2),
            Mod("c.esm", 3),
        };
        var current = new[]
        {
            Mod("a.esm",   1),
            Mod("new.esm", 2),
            Mod("b.esm",   3),
            Mod("c.esm",   4),
        };

        var lcs    = Lcs(reference, current);
        var result = DiffService.ClassifyChanges(reference, current, lcs);

        var changed = result.Where(d => d.ChangeType != DiffChangeType.Unchanged && d.ChangeType != DiffChangeType.Separator).ToList();
        var inserted = Assert.Single(changed);

        Assert.Equal(2, inserted.DependentChanges.Count);
        Assert.All(inserted.DependentChanges, dep => Assert.Equal(DiffChangeType.Moved, dep.ChangeType));
        Assert.Contains(inserted.DependentChanges, dep => dep.FileName == "b.esm");
        Assert.Contains(inserted.DependentChanges, dep => dep.FileName == "c.esm");
    }
}
