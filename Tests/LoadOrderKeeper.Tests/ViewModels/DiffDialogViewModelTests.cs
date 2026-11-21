using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using LoadOrderKeeper.Models;
using LoadOrderKeeper.Tests.Fixtures;
using LoadOrderKeeper.ViewModels;
using Xunit;

namespace LoadOrderKeeper.Tests.ViewModels;

/// <summary>
/// Unit tests for <see cref="DiffDialogViewModel"/> covering the guidance-sidebar
/// computed properties (visibility flags, mod counts, count display strings) and the
/// confirmation-message builder.
/// </summary>
/// <remarks>
/// All tests use the <c>internal</c> test-only constructor that accepts a
/// pre-populated <see cref="ObservableCollection{T}"/> of <see cref="DiffLineModel"/>
/// objects and skips the full coordinator/MainViewModel dependency graph.
/// <see cref="DiffDialogViewModel.BuildConfirmationMessage"/> is <c>internal</c>
/// and accessible via the <c>InternalsVisibleTo</c> attribute declared in the main
/// project file.
/// </remarks>
[Collection(LocaleSequentialCollection.Name)]
public sealed class DiffDialogViewModelTests : IClassFixture<EnglishLocaleFixture>
{
    public DiffDialogViewModelTests(EnglishLocaleFixture localeFixture)
    {
        _ = localeFixture; // Ensures en-US locale is active for all tests in this class
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private static DiffLineModel Line(DiffChangeType changeType, string name = "Mod.esm") =>
        new(name, name, changeType, referenceNumber: 1, currentNumber: 1);

    private static DiffDialogViewModel Create(IEnumerable<DiffLineModel> lines) =>
        new(new ObservableCollection<DiffLineModel>(lines));

    // ── ShowRemovalGuidance ──────────────────────────────────────────────────────

    [Fact]
    public void ShowRemovalGuidance_WhenRemovedModsPresent_ReturnsTrue()
    {
        // Arrange
        var vm = Create([Line(DiffChangeType.Removed)]);

        // Act & Assert
        Assert.True(vm.ShowRemovalGuidance);
    }

    [Fact]
    public void ShowRemovalGuidance_WhenNoRemovedMods_ReturnsFalse()
    {
        // Arrange: only safe changes
        var vm = Create([Line(DiffChangeType.Added)]);

        // Act & Assert
        Assert.False(vm.ShowRemovalGuidance);
    }

    // ── ShowInsertionGuidance ────────────────────────────────────────────────────

    [Fact]
    public void ShowInsertionGuidance_WhenInsertedModsPresent_ReturnsTrue()
    {
        // Arrange
        var vm = Create([Line(DiffChangeType.Inserted)]);

        // Act & Assert
        Assert.True(vm.ShowInsertionGuidance);
    }

    [Fact]
    public void ShowInsertionGuidance_WhenNoInsertedMods_ReturnsFalse()
    {
        // Arrange
        var vm = Create([Line(DiffChangeType.Added)]);

        // Act & Assert
        Assert.False(vm.ShowInsertionGuidance);
    }

    // ── ShowReplacementGuidance ──────────────────────────────────────────────────

    [Fact]
    public void ShowReplacementGuidance_WhenReplacedModsPresent_ReturnsTrue()
    {
        // Arrange
        var vm = Create([Line(DiffChangeType.Replaced)]);

        // Act & Assert
        Assert.True(vm.ShowReplacementGuidance);
    }

    [Fact]
    public void ShowReplacementGuidance_WhenNoReplacedMods_ReturnsFalse()
    {
        // Arrange
        var vm = Create([Line(DiffChangeType.Added)]);

        // Act & Assert
        Assert.False(vm.ShowReplacementGuidance);
    }

    // ── ShowAddedGuidance ────────────────────────────────────────────────────────

    [Fact]
    public void ShowAddedGuidance_WhenAddedModsPresent_ReturnsTrue()
    {
        // Arrange
        var vm = Create([Line(DiffChangeType.Added)]);

        // Act & Assert
        Assert.True(vm.ShowAddedGuidance);
    }

    [Fact]
    public void ShowAddedGuidance_WhenNoAddedMods_ReturnsFalse()
    {
        // Arrange
        var vm = Create([Line(DiffChangeType.Removed)]);

        // Act & Assert
        Assert.False(vm.ShowAddedGuidance);
    }

    // ── ShowNoChangesGuidance ────────────────────────────────────────────────────

    [Fact]
    public void ShowNoChangesGuidance_WhenNoDifferences_ReturnsTrue()
    {
        // Arrange: only Unchanged entries → HasDifferences = false
        var vm = Create([Line(DiffChangeType.Unchanged)]);

        // Act & Assert
        Assert.True(vm.ShowNoChangesGuidance);
        Assert.False(vm.HasDifferences);
    }

    [Fact]
    public void ShowNoChangesGuidance_WhenDifferencesExist_ReturnsFalse()
    {
        // Arrange
        var vm = Create([Line(DiffChangeType.Added)]);

        // Act & Assert
        Assert.False(vm.ShowNoChangesGuidance);
    }

    // ── Count properties ─────────────────────────────────────────────────────────

    [Fact]
    public void RemovedModCount_ReturnsCorrectCount()
    {
        // Arrange
        var vm = Create([
            Line(DiffChangeType.Removed, "Mod1.esm"),
            Line(DiffChangeType.Removed, "Mod2.esm"),
            Line(DiffChangeType.Added,   "Mod3.esm")
        ]);

        // Act & Assert
        Assert.Equal(2, vm.RemovedModCount);
    }

    [Fact]
    public void InsertedModCount_ReturnsCorrectCount()
    {
        // Arrange
        var vm = Create([
            Line(DiffChangeType.Inserted, "Mod1.esm"),
            Line(DiffChangeType.Added,    "Mod2.esm")
        ]);

        // Act & Assert
        Assert.Equal(1, vm.InsertedModCount);
    }

    [Fact]
    public void ReplacedModCount_ReturnsCorrectCount()
    {
        // Arrange
        var vm = Create([
            Line(DiffChangeType.Replaced, "Mod1.esm"),
            Line(DiffChangeType.Replaced, "Mod2.esm"),
            Line(DiffChangeType.Replaced, "Mod3.esm")
        ]);

        // Act & Assert
        Assert.Equal(3, vm.ReplacedModCount);
    }

    [Fact]
    public void AddedModCount_ReturnsCorrectCount()
    {
        // Arrange
        var vm = Create([
            Line(DiffChangeType.Added,   "Mod1.esm"),
            Line(DiffChangeType.Removed, "Mod2.esm")
        ]);

        // Act & Assert
        Assert.Equal(1, vm.AddedModCount);
    }

    // ── Count text — singular ────────────────────────────────────────────────────

    [Fact]
    public void RemovedModCountText_Singular()
    {
        // Arrange: exactly 1 removed mod
        var vm = Create([Line(DiffChangeType.Removed)]);

        // Act & Assert
        Assert.Equal("1 mod removed", vm.RemovedModCountText);
    }

    [Fact]
    public void InsertedModCountText_Singular()
    {
        // Arrange: exactly 1 inserted mod
        var vm = Create([Line(DiffChangeType.Inserted)]);

        // Act & Assert
        Assert.Equal("1 mod inserted", vm.InsertedModCountText);
    }

    [Fact]
    public void ReplacedModCountText_Singular()
    {
        // Arrange: exactly 1 replaced mod
        var vm = Create([Line(DiffChangeType.Replaced)]);

        // Act & Assert
        Assert.Equal("1 mod replaced", vm.ReplacedModCountText);
    }

    [Fact]
    public void AddedModCountText_Singular()
    {
        // Arrange: exactly 1 added mod
        var vm = Create([Line(DiffChangeType.Added)]);

        // Act & Assert
        Assert.Equal("1 mod added", vm.AddedModCountText);
    }

    // ── Count text — plural ──────────────────────────────────────────────────────

    [Fact]
    public void RemovedModCountText_Plural()
    {
        // Arrange: 3 removed mods → plural format string
        var vm = Create([
            Line(DiffChangeType.Removed, "Mod1.esm"),
            Line(DiffChangeType.Removed, "Mod2.esm"),
            Line(DiffChangeType.Removed, "Mod3.esm")
        ]);

        // Act & Assert
        Assert.Equal("3 mod(s) removed", vm.RemovedModCountText);
    }

    [Fact]
    public void InsertedModCountText_Plural()
    {
        // Arrange: 2 inserted mods → plural format string
        var vm = Create([
            Line(DiffChangeType.Inserted, "Mod1.esm"),
            Line(DiffChangeType.Inserted, "Mod2.esm")
        ]);

        // Act & Assert
        Assert.Equal("2 mod(s) inserted", vm.InsertedModCountText);
    }

    [Fact]
    public void ReplacedModCountText_Plural()
    {
        // Arrange: 2 replaced mods
        var vm = Create([
            Line(DiffChangeType.Replaced, "Mod1.esm"),
            Line(DiffChangeType.Replaced, "Mod2.esm")
        ]);

        // Act & Assert
        Assert.Equal("2 mod(s) replaced", vm.ReplacedModCountText);
    }

    [Fact]
    public void AddedModCountText_Plural()
    {
        // Arrange: 2 added mods
        var vm = Create([
            Line(DiffChangeType.Added, "Mod1.esm"),
            Line(DiffChangeType.Added, "Mod2.esm")
        ]);

        // Act & Assert
        Assert.Equal("2 mod(s) added", vm.AddedModCountText);
    }

    // ── Dynamic refresh (GuidanceVisibility_UpdatesAfterDiffRefresh) ─────────────

    [Fact]
    public void GuidanceVisibility_UpdatesAfterDiffRefresh()
    {
        // Arrange: start with a Removed mod
        var lines = new ObservableCollection<DiffLineModel>
        {
            Line(DiffChangeType.Removed, "Mod1.esm")
        };
        var vm = new DiffDialogViewModel(lines);

        Assert.True(vm.ShowRemovalGuidance,  "Precondition: removal guidance should be visible");
        Assert.False(vm.ShowAddedGuidance,   "Precondition: added guidance should be hidden");

        // Capture that PropertyChanged fires for Show* properties when the collection changes
        var changedProperties = new List<string?>();
        ((INotifyPropertyChanged)vm).PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        // Act: replace the removed line with an added line (simulates a diff refresh)
        lines.Clear();
        lines.Add(Line(DiffChangeType.Added, "NewMod.esm"));

        // Assert: visibility flipped correctly
        Assert.False(vm.ShowRemovalGuidance, "Removal guidance should now be hidden");
        Assert.True(vm.ShowAddedGuidance,    "Added guidance should now be visible");

        // Assert: property change notifications were raised.
        // Per CommunityToolkit.Mvvm convention, PropertyChanged with string.Empty (or null) signals
        // a full refresh of all properties — UpdateDiffState() uses this instead of individual
        // nameof(...) calls so that newly-added computed properties are covered automatically.
        Assert.Contains(string.Empty, changedProperties);
    }

    // ── HasReplacedMods ──────────────────────────────────────────────────────────

    [Fact]
    public void HasReplacedMods_WhenReplacedModsPresent_ReturnsTrue()
    {
        // Arrange
        var vm = Create([Line(DiffChangeType.Replaced)]);

        // Act & Assert
        Assert.True(vm.HasReplacedMods);
    }

    [Fact]
    public void HasReplacedMods_WhenNoReplacedMods_ReturnsFalse()
    {
        // Arrange
        var vm = Create([Line(DiffChangeType.Added)]);

        // Act & Assert
        Assert.False(vm.HasReplacedMods);
    }

    // ── Dispose() lifecycle ──────────────────────────────────────────────────────

    [Fact]
    public void Dispose_WithTestConstructor_DoesNotThrow()
    {
        // Arrange: _mainViewModel is null! in the test constructor
        var vm = Create([Line(DiffChangeType.Removed)]);

        // Act & Assert: Dispose() must not throw when _mainViewModel is null
        var ex = Record.Exception(() => vm.Dispose());
        Assert.Null(ex);
    }

    // ── ShowMovedGuidance ────────────────────────────────────────────────────────

    [Fact]
    public void ShowMovedGuidance_WhenMovedModsPresent_ReturnsTrue()
    {
        // Arrange
        var vm = Create([Line(DiffChangeType.Moved)]);

        // Act & Assert
        Assert.True(vm.ShowMovedGuidance);
    }

    [Fact]
    public void ShowMovedGuidance_WhenNoMovedMods_ReturnsFalse()
    {
        // Arrange: only Added lines — no Moved change type
        var vm = Create([Line(DiffChangeType.Added)]);

        // Act & Assert
        Assert.False(vm.ShowMovedGuidance);
    }

    // ── MovedModCount ─────────────────────────────────────────────────────────────

    [Fact]
    public void MovedModCount_ReturnsCorrectCount()
    {
        // Arrange
        var vm = Create([
            Line(DiffChangeType.Moved, "Mod1.esm"),
            Line(DiffChangeType.Moved, "Mod2.esm"),
            Line(DiffChangeType.Added, "Mod3.esm")
        ]);

        // Act & Assert
        Assert.Equal(2, vm.MovedModCount);
    }

    // ── MovedModCountText ─────────────────────────────────────────────────────────

    [Fact]
    public void MovedModCountText_Singular()
    {
        // Arrange: exactly 1 moved mod
        var vm = Create([Line(DiffChangeType.Moved)]);

        // Act & Assert
        Assert.Equal("1 mod moved", vm.MovedModCountText);
    }

    [Fact]
    public void MovedModCountText_Plural()
    {
        // Arrange: 3 moved mods → plural format string
        var vm = Create([
            Line(DiffChangeType.Moved, "Mod1.esm"),
            Line(DiffChangeType.Moved, "Mod2.esm"),
            Line(DiffChangeType.Moved, "Mod3.esm")
        ]);

        // Act & Assert
        Assert.Equal("3 mod(s) moved", vm.MovedModCountText);
    }

    // ── BuildConfirmationMessage ─────────────────────────────────────────────────

    [Fact]
    public void BuildConfirmationMessage_RiskyChanges_IncludesRemovedCount()
    {
        // Arrange: 2 Removed + 1 Inserted — both are risky change types
        var vm = Create([
            Line(DiffChangeType.Removed, "Mod1.esm"),
            Line(DiffChangeType.Removed, "Mod2.esm"),
            Line(DiffChangeType.Inserted, "Mod3.esm")
        ]);

        // Act
        string? result = vm.BuildConfirmationMessage();

        // Assert: result is non-null and contains both the removed and inserted detail strings
        Assert.NotNull(result);
        Assert.Contains("2 removed mod(s)", result);
        Assert.Contains("1 inserted mod", result);
    }

    [Fact]
    public void BuildConfirmationMessage_SafeChanges_ReturnsNull()
    {
        // Arrange: only Added and Replaced lines — no risky change types
        var vm = Create([
            Line(DiffChangeType.Added,    "Mod1.esm"),
            Line(DiffChangeType.Replaced, "Mod2.esm")
        ]);

        // Act
        string? result = vm.BuildConfirmationMessage();

        // Assert: null signals "no confirmation needed"
        Assert.Null(result);
    }

    // ── Card expansion defaults (AC-05) ──────────────────────────────────────────

    [Fact]
    public void IsRemovalCardExpanded_DefaultsToTrue()
    {
        var vm = Create([Line(DiffChangeType.Removed)]);
        Assert.True(vm.IsRemovalCardExpanded);
    }

    [Fact]
    public void IsInsertionCardExpanded_DefaultsToFalse()
    {
        var vm = Create([Line(DiffChangeType.Inserted)]);
        Assert.False(vm.IsInsertionCardExpanded);
    }

    [Fact]
    public void IsReplacementCardExpanded_DefaultsToFalse()
    {
        var vm = Create([Line(DiffChangeType.Replaced)]);
        Assert.False(vm.IsReplacementCardExpanded);
    }

    [Fact]
    public void IsAddedCardExpanded_DefaultsToFalse()
    {
        var vm = Create([Line(DiffChangeType.Added)]);
        Assert.False(vm.IsAddedCardExpanded);
    }

    [Fact]
    public void IsMovedCardExpanded_DefaultsToFalse()
    {
        var vm = Create([Line(DiffChangeType.Moved)]);
        Assert.False(vm.IsMovedCardExpanded);
    }

    [Fact]
    public void IsNoChangesCardExpanded_DefaultsToFalse()
    {
        var vm = Create([Line(DiffChangeType.Unchanged)]);
        Assert.False(vm.IsNoChangesCardExpanded);
    }

    // ── Card toggle commands (AC-04) ─────────────────────────────────────────────

    [Fact]
    public void ToggleRemovalCardCommand_InvertsIsRemovalCardExpanded()
    {
        var vm = Create([Line(DiffChangeType.Removed)]);
        bool initial = vm.IsRemovalCardExpanded; // true by default
        vm.ToggleRemovalCardCommand.Execute(null);
        Assert.Equal(!initial, vm.IsRemovalCardExpanded);
    }

    [Fact]
    public void ToggleInsertionCardCommand_InvertsIsInsertionCardExpanded()
    {
        var vm = Create([Line(DiffChangeType.Inserted)]);
        bool initial = vm.IsInsertionCardExpanded;
        vm.ToggleInsertionCardCommand.Execute(null);
        Assert.Equal(!initial, vm.IsInsertionCardExpanded);
    }

    [Fact]
    public void ToggleReplacementCardCommand_InvertsIsReplacementCardExpanded()
    {
        var vm = Create([Line(DiffChangeType.Replaced)]);
        bool initial = vm.IsReplacementCardExpanded;
        vm.ToggleReplacementCardCommand.Execute(null);
        Assert.Equal(!initial, vm.IsReplacementCardExpanded);
    }

    [Fact]
    public void ToggleAddedCardCommand_InvertsIsAddedCardExpanded()
    {
        var vm = Create([Line(DiffChangeType.Added)]);
        bool initial = vm.IsAddedCardExpanded;
        vm.ToggleAddedCardCommand.Execute(null);
        Assert.Equal(!initial, vm.IsAddedCardExpanded);
    }

    [Fact]
    public void ToggleMovedCardCommand_InvertsIsMovedCardExpanded()
    {
        var vm = Create([Line(DiffChangeType.Moved)]);
        bool initial = vm.IsMovedCardExpanded;
        vm.ToggleMovedCardCommand.Execute(null);
        Assert.Equal(!initial, vm.IsMovedCardExpanded);
    }

    [Fact]
    public void ToggleNoChangesCardCommand_InvertsIsNoChangesCardExpanded()
    {
        var vm = Create([Line(DiffChangeType.Unchanged)]);
        bool initial = vm.IsNoChangesCardExpanded;
        vm.ToggleNoChangesCardCommand.Execute(null);
        Assert.Equal(!initial, vm.IsNoChangesCardExpanded);
    }
}
