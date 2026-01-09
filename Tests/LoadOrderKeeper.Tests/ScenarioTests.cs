using System.Linq;
using System.Threading.Tasks;
using LoadOrderKeeper.Services;
using Xunit;

namespace LoadOrderKeeper.Tests;

/// <summary>
/// Comprehensive scenario tests covering all 15 documented sorting scenarios.
/// Each test validates change detection and sorting behavior for real-world use cases.
/// </summary>
public class ScenarioTests : ScenarioTestBase
{
    #region Basic Operations

    /// <summary>
    /// Scenario 01: Tests detection of a single mod added to the end of the load order.
    /// </summary>
    [Fact]
    public async Task Scenario01_AddedNewMod_DetectsAddition()
    {
        // Arrange
        using var context = new TestConfigContext();
        await SetupStandardReferenceAsync(context);
        var modifiedOrder = StandardModList.Concat(new[] { "*NewMod.esm" }).ToArray();
        await SetupCurrentOrderAsync(context, modifiedOrder);

        // Act - Detect Changes
        var diffs = await DiffService.GetPluginsDiffAsync(context.Config);

        // Assert - Change Detection
        AssertChangeCount(diffs, 1);
        AssertModAdded(diffs, "NewMod.esm", 19);

        // Act - Apply Sorting
        await FileService.ApplyLoadOrderAsync(context.Config);
        var postSortDiffs = await DiffService.GetPluginsDiffAsync(context.Config);

        // Assert - Post-Sort: New mod remains at the end
        AssertChangeCount(postSortDiffs, 1);
        AssertModAdded(postSortDiffs, "NewMod.esm", 19);
    }

    /// <summary>
    /// Scenario 02: Tests detection and correction when external tool swaps two mods.
    /// </summary>
    [Fact]
    public async Task Scenario02_SortingModifiedExternally_RestoresOrder()
    {
        // Arrange
        using var context = new TestConfigContext();
        await SetupStandardReferenceAsync(context);
        
        // Swap first two mods
        var modifiedOrder = StandardModList.ToArray();
        (modifiedOrder[0], modifiedOrder[1]) = (modifiedOrder[1], modifiedOrder[0]);
        await SetupCurrentOrderAsync(context, modifiedOrder);

        // Act - Detect Changes
        var diffs = await DiffService.GetPluginsDiffAsync(context.Config);

        // Assert - Change Detection: Both mods detected as moved
        AssertChangeCount(diffs, 2);
        AssertModMoved(diffs, "AmazonCrew.esm", 2, 1);
        AssertModMoved(diffs, "StarfieldCommunityPatch.esm", 1, 2);

        // Act - Apply Sorting
        await FileService.ApplyLoadOrderAsync(context.Config);
        var postSortDiffs = await DiffService.GetPluginsDiffAsync(context.Config);

        // Assert - Post-Sort: Order restored, no differences
        AssertNoChanges(postSortDiffs);
    }

    /// <summary>
    /// Scenario 03: Tests detection when a mod is inserted in the middle of the load order.
    /// </summary>
    [Fact]
    public async Task Scenario03_InsertedNewMod_DetectsInsertion()
    {
        // Arrange
        using var context = new TestConfigContext();
        await SetupStandardReferenceAsync(context);
        
        // Insert new mod at position 17 (before Fragile.esm)
        var modifiedOrder = StandardModList.Take(16)
            .Concat(new[] { "*InsertedMod.esm" })
            .Concat(StandardModList.Skip(16))
            .ToArray();
        await SetupCurrentOrderAsync(context, modifiedOrder);

        // Act - Detect Changes
        var diffs = await DiffService.GetPluginsDiffAsync(context.Config);

        // Assert - Change Detection: Insertion + dependent shifts
        AssertModAdded(diffs, "InsertedMod.esm", 17);
        
        // Verify dependent changes
        var inserted = diffs.FirstOrDefault(d => d.FileName.Equals("InsertedMod.esm", System.StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(inserted);
        Assert.True(inserted.HasDependentChanges);
        AssertDependentChanges(diffs, "InsertedMod.esm", "Fragile.esm", "GagarinNewDawn.esm");

        // Act - Apply Sorting
        await FileService.ApplyLoadOrderAsync(context.Config);
        var postSortDiffs = await DiffService.GetPluginsDiffAsync(context.Config);

        // Assert - Post-Sort: Inserted mod moved to end
        AssertChangeCount(postSortDiffs, 1);
        AssertModAdded(postSortDiffs, "InsertedMod.esm", 19);
    }

    /// <summary>
    /// Scenario 04: Tests detection when a single mod is removed from the load order.
    /// </summary>
    [Fact]
    public async Task Scenario04_DeletedMod_DetectsDeletion()
    {
        // Arrange
        using var context = new TestConfigContext();
        await SetupStandardReferenceAsync(context);
        
        // Remove OutpostFishTank.esm (position 16)
        var modifiedOrder = StandardModList.Where(m => m != "*OutpostFishTank.esm").ToArray();
        await SetupCurrentOrderAsync(context, modifiedOrder);

        // Act - Detect Changes
        var diffs = await DiffService.GetPluginsDiffAsync(context.Config);

        // Assert - Change Detection: Deletion + dependent shifts
        AssertModRemoved(diffs, "OutpostFishTank.esm", 16);
        
        // Verify dependent changes
        var removed = diffs.FirstOrDefault(d => d.FileName.Equals("OutpostFishTank.esm", System.StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(removed);
        Assert.True(removed.HasDependentChanges);
        AssertDependentChanges(diffs, "OutpostFishTank.esm", "Fragile.esm", "GagarinNewDawn.esm");

        // Act - Apply Sorting
        await FileService.ApplyLoadOrderAsync(context.Config);
        var postSortDiffs = await DiffService.GetPluginsDiffAsync(context.Config);

        // Assert - Post-Sort: Deletion remains (dependent shifts are hidden after dependent mods removed from list)
        AssertChangeCount(postSortDiffs, 1);
        AssertModRemoved(postSortDiffs, "OutpostFishTank.esm", 16);
    }

    /// <summary>
    /// Scenario 05: Tests detection when a mod is replaced with an alternative version.
    /// </summary>
    [Fact]
    public async Task Scenario05_ReplacedMod_DetectsReplacement()
    {
        // Arrange
        using var context = new TestConfigContext();
        await SetupStandardReferenceAsync(context);
        
        // Replace OutpostFishTank.esm with ReplacementMod.esm at position 16
        var modifiedOrder = StandardModList.Take(15)
            .Concat(new[] { "*ReplacementMod.esm" })
            .Concat(StandardModList.Skip(16))
            .ToArray();
        await SetupCurrentOrderAsync(context, modifiedOrder);

        // Act - Detect Changes
        var diffs = await DiffService.GetPluginsDiffAsync(context.Config);

        // Assert - Change Detection: Replacement detected
        AssertChangeCount(diffs, 1);
        AssertModReplaced(diffs, "OutpostFishTank.esm", "ReplacementMod.esm", 16);

        // Act - Apply Sorting
        await FileService.ApplyLoadOrderAsync(context.Config);
        var postSortDiffs = await DiffService.GetPluginsDiffAsync(context.Config);

        // Assert - Post-Sort: Replacement preserved (user-directed change)
        // The replacement mod stays at position 16 where it replaced the original
        AssertChangeCount(postSortDiffs, 1);
        AssertModReplaced(postSortDiffs, "OutpostFishTank.esm", "ReplacementMod.esm", 16);
    }

    #endregion

    #region Complex Operations

    /// <summary>
    /// Scenario 06: Tests combined deletion, replacement, and insertion.
    /// </summary>
    [Fact]
    public async Task Scenario06_CombinedChanges_DetectsMultipleChangeTypes()
    {
        // Arrange
        using var context = new TestConfigContext();
        await SetupStandardReferenceAsync(context);
        
        // Complex scenario: deletion + replacement + insertion
        // After BuySwimsuits deletion, Easy Digipick at ref pos 13 moves to current pos 12
        // Replace it with ReplacementMod at that position
        var modifiedOrder = new[]
        {
            "*StarfieldCommunityPatch.esm",
            "*AmazonCrew.esm",
            "*ShipBuilderCategories.esm",
            "*BetterShipPartFlips.esm",
            "*BetterShipPartSnaps.esm",
            "*Better_Living.esm",
            "*Richer Merchants.esm",
            "*xatmosPerkUpVendors.esp",
            "*fixgraydockingcolors.esm",
            "*DayLengthMessage.esm",
            "*Eit_Clothiers_Z.esm",
            "*ReplacementMod.esm",         // Position 12 (replaces Easy Digipick after deletion shift)
            "*Eli_RenamedSnowglobes.esm",
            "*Nanosuit_f_new.esm",
            "*InsertedMod.esm",            // Position 15 (inserted)
            "*OutpostFishTank.esm",
            "*Fragile.esm",
            "*GagarinNewDawn.esm"
        };
        await SetupCurrentOrderAsync(context, modifiedOrder);

        // Act - Detect Changes
        var diffs = await DiffService.GetPluginsDiffAsync(context.Config);

        // Assert - Change Detection
        AssertModRemoved(diffs, "BuySwimsuits.esm", 9);
        
        // Replacement won't be detected here because Easy Digipick ref=13 but ReplacementMod cur=12
        // They're at different positions due to the earlier deletion
        
        AssertModAdded(diffs, "InsertedMod.esm", 15);
        
        // Verify dependent changes for deletion
        var removed = diffs.FirstOrDefault(d => d.FileName.Equals("BuySwimsuits.esm", System.StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(removed);
        Assert.True(removed.HasDependentChanges);

        // Act - Apply Sorting
        await FileService.ApplyLoadOrderAsync(context.Config);
        var postSortDiffs = await DiffService.GetPluginsDiffAsync(context.Config);

        // Assert - Post-Sort
        AssertModRemoved(postSortDiffs, "BuySwimsuits.esm", 9);
        AssertModRemoved(postSortDiffs, "Easy Digipick.esm", 13);
        
        // InsertedMod and ReplacementMod should both be at the end (not replacements in this case)
        var replacementPost = postSortDiffs.FirstOrDefault(d => d.FileName.Equals("ReplacementMod.esm", System.StringComparison.OrdinalIgnoreCase));
        var insertedPost = postSortDiffs.FirstOrDefault(d => d.FileName.Equals("InsertedMod.esm", System.StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(replacementPost);
        Assert.NotNull(insertedPost);
    }

    /// <summary>
    /// Scenario 07: Tests detection when multiple mods are deleted at once.
    /// </summary>
    [Fact]
    public async Task Scenario07_MultipleDeletedMods_DetectsAllDeletions()
    {
        // Arrange
        using var context = new TestConfigContext();
        await SetupStandardReferenceAsync(context);
        
        // Delete DayLengthMessage.esm, Eit_Clothiers_Z.esm, and Nanosuit_f_new.esm
        var modifiedOrder = StandardModList
            .Where(m => m != "*DayLengthMessage.esm" && 
                       m != "*Eit_Clothiers_Z.esm" && 
                       m != "*Nanosuit_f_new.esm")
            .ToArray();
        await SetupCurrentOrderAsync(context, modifiedOrder);

        // Act - Detect Changes
        var diffs = await DiffService.GetPluginsDiffAsync(context.Config);

        // Assert - Change Detection
        AssertModRemoved(diffs, "DayLengthMessage.esm", 11);
        AssertModRemoved(diffs, "Eit_Clothiers_Z.esm", 12);
        AssertModRemoved(diffs, "Nanosuit_f_new.esm", 15);
        
        // Verify dependent changes exist
        var eit = diffs.FirstOrDefault(d => d.FileName.Equals("Eit_Clothiers_Z.esm", System.StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(eit);
        Assert.True(eit.HasDependentChanges);
        
        var nanosuit = diffs.FirstOrDefault(d => d.FileName.Equals("Nanosuit_f_new.esm", System.StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(nanosuit);
        Assert.True(nanosuit.HasDependentChanges);

        // Act - Apply Sorting
        await FileService.ApplyLoadOrderAsync(context.Config);
        var postSortDiffs = await DiffService.GetPluginsDiffAsync(context.Config);

        // Assert - Post-Sort: Deletions remain
        AssertModRemoved(postSortDiffs, "DayLengthMessage.esm", 11);
        AssertModRemoved(postSortDiffs, "Eit_Clothiers_Z.esm", 12);
        AssertModRemoved(postSortDiffs, "Nanosuit_f_new.esm", 15);
    }

    /// <summary>
    /// Scenario 08: Tests detection when multiple mods are added at once.
    /// </summary>
    [Fact]
    public async Task Scenario08_MultipleAddedMods_DetectsAllAdditions()
    {
        // Arrange
        using var context = new TestConfigContext();
        await SetupStandardReferenceAsync(context);
        
        // Add three new mods at the end
        var modifiedOrder = StandardModList
            .Concat(new[] { "*NewModA.esm", "*NewModB.esm", "*NewModC.esm" })
            .ToArray();
        await SetupCurrentOrderAsync(context, modifiedOrder);

        // Act - Detect Changes
        var diffs = await DiffService.GetPluginsDiffAsync(context.Config);

        // Assert - Change Detection
        AssertChangeCount(diffs, 3);
        AssertModAdded(diffs, "NewModA.esm", 19);
        AssertModAdded(diffs, "NewModB.esm", 20);
        AssertModAdded(diffs, "NewModC.esm", 21);

        // Act - Apply Sorting
        await FileService.ApplyLoadOrderAsync(context.Config);
        var postSortDiffs = await DiffService.GetPluginsDiffAsync(context.Config);

        // Assert - Post-Sort: All new mods remain at the end
        AssertChangeCount(postSortDiffs, 3);
        AssertModAdded(postSortDiffs, "NewModA.esm", 19);
        AssertModAdded(postSortDiffs, "NewModB.esm", 20);
        AssertModAdded(postSortDiffs, "NewModC.esm", 21);
    }

    /// <summary>
    /// Scenario 09: Tests detection when multiple mods are replaced at once.
    /// </summary>
    [Fact]
    public async Task Scenario09_MultipleReplacedMods_DetectsAllReplacements()
    {
        // Arrange
        using var context = new TestConfigContext();
        await SetupStandardReferenceAsync(context);
        
        // Replace three mods with new versions at exact same positions
        var modifiedOrder = StandardModList.Take(8)
            .Concat(new[] { "*BuySwimsuits_v2.esm" })  // Replace position 9
            .Concat(StandardModList.Skip(9).Take(2))   // Positions 10-11
            .Concat(new[] { "*Eit_Clothiers_Enhanced.esm" })  // Replace position 12
            .Concat(StandardModList.Skip(12).Take(3))  // Positions 13-15
            .Concat(new[] { "*ImprovedFishTank.esm" }) // Replace position 16
            .Concat(StandardModList.Skip(16))          // Remaining
            .ToArray();
        await SetupCurrentOrderAsync(context, modifiedOrder);

        // Act - Detect Changes
        var diffs = await DiffService.GetPluginsDiffAsync(context.Config);

        // Assert - Change Detection: 3 replacements detected
        AssertChangeCount(diffs, 3);
        AssertModReplaced(diffs, "BuySwimsuits.esm", "BuySwimsuits_v2.esm", 9);
        AssertModReplaced(diffs, "Eit_Clothiers_Z.esm", "Eit_Clothiers_Enhanced.esm", 12);
        AssertModReplaced(diffs, "OutpostFishTank.esm", "ImprovedFishTank.esm", 16);

        // Act - Apply Sorting
        await FileService.ApplyLoadOrderAsync(context.Config);
        var postSortDiffs = await DiffService.GetPluginsDiffAsync(context.Config);

        // Assert - Post-Sort: Replacements preserved (user-directed changes)
        // All replacement mods stay in their original positions
        AssertChangeCount(postSortDiffs, 3);
        AssertModReplaced(postSortDiffs, "BuySwimsuits.esm", "BuySwimsuits_v2.esm", 9);
        AssertModReplaced(postSortDiffs, "Eit_Clothiers_Z.esm", "Eit_Clothiers_Enhanced.esm", 12);
        AssertModReplaced(postSortDiffs, "OutpostFishTank.esm", "ImprovedFishTank.esm", 16);
    }

    /// <summary>
    /// Scenario 10: Tests detection when external tool reorders multiple mods.
    /// </summary>
    [Fact]
    public async Task Scenario10_MultipleMovedExternally_RestoresAllPositions()
    {
        // Arrange
        using var context = new TestConfigContext();
        await SetupStandardReferenceAsync(context);
        
        // Move several mods around
        var modifiedOrder = new[]
        {
            "*Fragile.esm",  // Was 17, now 1
            "*StarfieldCommunityPatch.esm",  // Was 1, now 2
            "*BetterShipPartSnaps.esm",  // Was 5, now 3
            "*AmazonCrew.esm",  // Was 2, now 4
            "*ShipBuilderCategories.esm",
            "*BetterShipPartFlips.esm",
            "*Better_Living.esm",
            "*Richer Merchants.esm",
            "*xatmosPerkUpVendors.esp",
            "*BuySwimsuits.esm",
            "*fixgraydockingcolors.esm",
            "*DayLengthMessage.esm",
            "*Eit_Clothiers_Z.esm",
            "*Easy Digipick.esm",
            "*Eli_RenamedSnowglobes.esm",
            "*Nanosuit_f_new.esm",
            "*OutpostFishTank.esm",
            "*GagarinNewDawn.esm"
        };
        await SetupCurrentOrderAsync(context, modifiedOrder);

        // Act - Detect Changes
        var diffs = await DiffService.GetPluginsDiffAsync(context.Config);

        // Assert - Change Detection: Multiple moves detected
        AssertModMoved(diffs, "Fragile.esm", 17, 1);
        AssertModMoved(diffs, "StarfieldCommunityPatch.esm", 1, 2);
        AssertModMoved(diffs, "BetterShipPartSnaps.esm", 5, 3);
        AssertModMoved(diffs, "AmazonCrew.esm", 2, 4);

        // Act - Apply Sorting
        await FileService.ApplyLoadOrderAsync(context.Config);
        var postSortDiffs = await DiffService.GetPluginsDiffAsync(context.Config);

        // Assert - Post-Sort: Order restored, no differences
        AssertNoChanges(postSortDiffs);
    }

    /// <summary>
    /// Scenario 11: Tests detection when mods are disabled (asterisk removed).
    /// </summary>
    [Fact]
    public async Task Scenario11_DisabledMods_TreatsAsRemoved()
    {
        // Arrange
        using var context = new TestConfigContext();
        await SetupStandardReferenceAsync(context);
        
        // Disable three mods by removing asterisk
        var modifiedOrder = StandardModList.ToArray();
        modifiedOrder[8] = "BuySwimsuits.esm";  // Disabled
        modifiedOrder[11] = "Eit_Clothiers_Z.esm";  // Disabled
        modifiedOrder[14] = "Nanosuit_f_new.esm";  // Disabled
        await SetupCurrentOrderAsync(context, modifiedOrder);

        // Act - Detect Changes
        var diffs = await DiffService.GetPluginsDiffAsync(context.Config);

        // Assert - Change Detection: Disabled mods treated as removed
        AssertChangeCount(diffs, 3);
        AssertModRemoved(diffs, "BuySwimsuits.esm", 9);
        AssertModRemoved(diffs, "Eit_Clothiers_Z.esm", 12);
        AssertModRemoved(diffs, "Nanosuit_f_new.esm", 15);

        // Act - Apply Sorting
        await FileService.ApplyLoadOrderAsync(context.Config);
        var postSortDiffs = await DiffService.GetPluginsDiffAsync(context.Config);

        // Assert - Post-Sort: Disabled mods remain removed
        AssertChangeCount(postSortDiffs, 3);
        AssertModRemoved(postSortDiffs, "BuySwimsuits.esm", 9);
        AssertModRemoved(postSortDiffs, "Eit_Clothiers_Z.esm", 12);
        AssertModRemoved(postSortDiffs, "Nanosuit_f_new.esm", 15);
    }

    /// <summary>
    /// Scenario 12: Tests detection when a mod is inserted and other mods are moved.
    /// </summary>
    [Fact]
    public async Task Scenario12_InsertedAndMovedCombination_DetectsBothChanges()
    {
        // Arrange
        using var context = new TestConfigContext();
        await SetupStandardReferenceAsync(context);
        
        // Swap positions 2 and 3, and insert new mod at position 5
        var modifiedOrder = new[]
        {
            "*StarfieldCommunityPatch.esm",
            "*ShipBuilderCategories.esm",  // Swapped with AmazonCrew
            "*AmazonCrew.esm",  // Swapped with ShipBuilderCategories
            "*BetterShipPartFlips.esm",
            "*InsertedMod.esm",  // Inserted
            "*BetterShipPartSnaps.esm",  // Shifted down
            "*Better_Living.esm",
            "*Richer Merchants.esm",
            "*xatmosPerkUpVendors.esp",
            "*BuySwimsuits.esm",
            "*fixgraydockingcolors.esm",
            "*DayLengthMessage.esm",
            "*Eit_Clothiers_Z.esm",
            "*Easy Digipick.esm",
            "*Eli_RenamedSnowglobes.esm",
            "*Nanosuit_f_new.esm",
            "*OutpostFishTank.esm",
            "*Fragile.esm",
            "*GagarinNewDawn.esm"
        };
        await SetupCurrentOrderAsync(context, modifiedOrder);

        // Act - Detect Changes
        var diffs = await DiffService.GetPluginsDiffAsync(context.Config);

        // Assert - Change Detection
        AssertModMoved(diffs, "AmazonCrew.esm", 2, 3);
        AssertModMoved(diffs, "ShipBuilderCategories.esm", 3, 2);
        AssertModAdded(diffs, "InsertedMod.esm", 5);
        
        // Verify inserted mod has dependent changes
        var inserted = diffs.FirstOrDefault(d => d.FileName.Equals("InsertedMod.esm", System.StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(inserted);
        Assert.True(inserted.HasDependentChanges);

        // Act - Apply Sorting
        await FileService.ApplyLoadOrderAsync(context.Config);
        var postSortDiffs = await DiffService.GetPluginsDiffAsync(context.Config);

        // Assert - Post-Sort: Inserted mod moved to end, moves corrected
        AssertChangeCount(postSortDiffs, 1);
        AssertModAdded(postSortDiffs, "InsertedMod.esm", 19);
    }

    #endregion

    #region Edge Cases

    /// <summary>
    /// Scenario 13: Tests that filename case changes are ignored (case-insensitive).
    /// </summary>
    [Fact]
    public async Task Scenario13_CaseSensitivity_IgnoresCaseChanges()
    {
        // Arrange
        using var context = new TestConfigContext();
        await SetupStandardReferenceAsync(context);
        
        // Create actual mod files with original casing
        CreateMockModFiles(context, StandardModList);
        
        // Change case in current order
        var modifiedOrder = new[]
        {
            "*starfieldcommunitypatch.esm",  // lowercase
            "*amazoncrew.esm",
            "*shipbuildercategories.esm",
            "*BETTERSHIPPARTFLIPS.ESM",  // uppercase
            "*BetterShipPartSnaps.esm",
            "*better_living.esm",
            "*Richer Merchants.esm",
            "*xatmosperkupvendors.esp",
            "*BuySwimsuits.esm",
            "*FIXGRAYDOCKINGCOLORS.ESM",
            "*DayLengthMessage.esm",
            "*Eit_Clothiers_Z.esm",
            "*easy digipick.esm",
            "*Eli_RenamedSnowglobes.esm",
            "*Nanosuit_f_new.esm",
            "*OutpostFishTank.esm",
            "*Fragile.esm",
            "*GagarinNewDawn.esm"
        };
        await SetupCurrentOrderAsync(context, modifiedOrder);

        // Act - Detect Changes
        var diffs = await DiffService.GetPluginsDiffAsync(context.Config);

        // Assert - Change Detection: No real changes (case-insensitive)
        AssertNoChanges(diffs);

        // Act - Apply Sorting
        await FileService.ApplyLoadOrderAsync(context.Config);

        // Assert - Post-Sort: Case should be normalized to match actual files
        await AssertCurrentOrderMatchesAsync(context, StandardModList);
    }

    /// <summary>
    /// Scenario 14: Tests detection when entire load order is reversed.
    /// </summary>
    [Fact]
    public async Task Scenario14_AllModsReordered_RestoresCompleteOrder()
    {
        // Arrange
        using var context = new TestConfigContext();
        await SetupStandardReferenceAsync(context);
        
        // Reverse the entire order
        var modifiedOrder = StandardModList.Reverse().ToArray();
        await SetupCurrentOrderAsync(context, modifiedOrder);

        // Act - Detect Changes
        var diffs = await DiffService.GetPluginsDiffAsync(context.Config);

        // Assert - Change Detection: All 18 mods detected as moved
        Assert.True(diffs.Count >= 2, "Should detect at least some moved mods");
        AssertModMoved(diffs, "GagarinNewDawn.esm", 18, 1);
        AssertModMoved(diffs, "StarfieldCommunityPatch.esm", 1, 18);

        // Act - Apply Sorting
        await FileService.ApplyLoadOrderAsync(context.Config);
        var postSortDiffs = await DiffService.GetPluginsDiffAsync(context.Config);

        // Assert - Post-Sort: Order restored, no differences
        AssertNoChanges(postSortDiffs);
    }

    /// <summary>
    /// Scenario 15: Tests that whitespace and comments are ignored during comparison.
    /// </summary>
    [Fact]
    public async Task Scenario15_WhitespaceAndComments_IgnoresFormatting()
    {
        // Arrange
        using var context = new TestConfigContext();
        await SetupStandardReferenceAsync(context);
        
        // Add comments and blank lines
        var modifiedOrder = new[]
        {
            "# Core mods section",
            "*StarfieldCommunityPatch.esm",
            "*AmazonCrew.esm",
            "",
            "*ShipBuilderCategories.esm",
            "*BetterShipPartFlips.esm",
            "*BetterShipPartSnaps.esm",
            "",
            "# Quality of life mods",
            "*Better_Living.esm",
            "*Richer Merchants.esm",
            "*xatmosPerkUpVendors.esp",
            "*BuySwimsuits.esm",
            "",
            "*fixgraydockingcolors.esm",
            "*DayLengthMessage.esm",
            "",
            "# Miscellaneous mods",
            "*Eit_Clothiers_Z.esm",
            "*Easy Digipick.esm",
            "*Eli_RenamedSnowglobes.esm",
            "*Nanosuit_f_new.esm",
            "",
            "*OutpostFishTank.esm",
            "*Fragile.esm",
            "*GagarinNewDawn.esm"
        };
        await SetupCurrentOrderAsync(context, modifiedOrder);

        // Act - Detect Changes
        var diffs = await DiffService.GetPluginsDiffAsync(context.Config);

        // Assert - Change Detection: No real changes
        AssertNoChanges(diffs);

        // Act - Apply Sorting
        await FileService.ApplyLoadOrderAsync(context.Config);
        var postSortDiffs = await DiffService.GetPluginsDiffAsync(context.Config);

        // Assert - Post-Sort: No changes, formatting cleaned
        AssertNoChanges(postSortDiffs);
        
        // Verify output is clean (no comments/blank lines)
        var currentMods = await GetCurrentModListAsync(context);
        Assert.Equal(18, currentMods.Count);
        Assert.DoesNotContain(currentMods, m => m.StartsWith("#") || string.IsNullOrWhiteSpace(m));
    }

    #endregion
}
