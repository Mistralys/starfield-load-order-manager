using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LoadOrderKeeper.Models;
using LoadOrderKeeper.Services;
using Xunit;

namespace LoadOrderKeeper.Tests;

/// <summary>
/// Base class for scenario tests providing common test infrastructure and assertion helpers.
/// </summary>
public abstract class ScenarioTestBase
{
    /// <summary>
    /// Standard 18-mod reference list used across all scenarios.
    /// </summary>
    protected static readonly string[] StandardModList = new[]
    {
        "*StarfieldCommunityPatch.esm",
        "*AmazonCrew.esm",
        "*ShipBuilderCategories.esm",
        "*BetterShipPartFlips.esm",
        "*BetterShipPartSnaps.esm",
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

    /// <summary>
    /// Sets up the standard 18-mod reference file.
    /// </summary>
    protected static async Task SetupStandardReferenceAsync(TestConfigContext context)
    {
        await context.WriteReferenceAsync(StandardModList);
    }

    /// <summary>
    /// Sets up a current order from a custom list of mods.
    /// </summary>
    protected static async Task SetupCurrentOrderAsync(TestConfigContext context, params string[] mods)
    {
        await context.WritePluginsAsync(mods);
    }

    /// <summary>
    /// Creates mock mod files in the Data folder for case sensitivity tests.
    /// </summary>
    protected static void CreateMockModFiles(TestConfigContext context, params string[] modNames)
    {
        var dataFolder = Path.Combine(context.StarfieldGamePath, "Data");
        Directory.CreateDirectory(dataFolder);

        foreach (var modName in modNames)
        {
            var modPath = Path.Combine(dataFolder, modName.TrimStart('*'));
            File.WriteAllText(modPath, string.Empty);
        }
    }

    #region Assertion Helpers

    /// <summary>
    /// Asserts that a mod was detected as added at the specified position.
    /// </summary>
    protected static void AssertModAdded(IReadOnlyList<DiffLineModel> diffs, string modName, int currentPosition)
    {
        var diff = FindDiff(diffs, modName);
        Assert.NotNull(diff);
        Assert.True(diff.ChangeType == DiffChangeType.Added || diff.ChangeType == DiffChangeType.Inserted, 
            $"Expected {modName} to be marked as Added or Inserted, but was {diff.ChangeType}");
        Assert.Equal(currentPosition, diff.CurrentNumber);
        Assert.Null(diff.ReferenceNumber);
    }

    /// <summary>
    /// Asserts that a mod was detected as removed from the specified reference position.
    /// </summary>
    protected static void AssertModRemoved(IReadOnlyList<DiffLineModel> diffs, string modName, int referencePosition)
    {
        var diff = FindDiff(diffs, modName);
        Assert.NotNull(diff);
        Assert.Equal(DiffChangeType.Removed, diff.ChangeType);
        Assert.Equal(referencePosition, diff.ReferenceNumber);
        Assert.Null(diff.CurrentNumber);
    }

    /// <summary>
    /// Asserts that a mod was detected as moved from one position to another.
    /// </summary>
    protected static void AssertModMoved(IReadOnlyList<DiffLineModel> diffs, string modName, int fromPosition, int toPosition)
    {
        var diff = FindDiff(diffs, modName);
        Assert.NotNull(diff);
        Assert.Equal(DiffChangeType.Moved, diff.ChangeType);
        Assert.Equal(fromPosition, diff.ReferenceNumber);
        Assert.Equal(toPosition, diff.CurrentNumber);
    }

    /// <summary>
    /// Asserts that a mod was detected as replaced at the specified position.
    /// </summary>
    protected static void AssertModReplaced(IReadOnlyList<DiffLineModel> diffs, string oldModName, string newModName, int position)
    {
        var diff = FindDiff(diffs, oldModName);
        Assert.NotNull(diff);
        Assert.Equal(DiffChangeType.Replaced, diff.ChangeType);
        Assert.Contains(oldModName, diff.Text, System.StringComparison.OrdinalIgnoreCase);
        Assert.Contains(newModName, diff.Text, System.StringComparison.OrdinalIgnoreCase);
        Assert.Equal(position, diff.ReferenceNumber);
    }

    /// <summary>
    /// Asserts that a mod has the expected dependent changes.
    /// </summary>
    protected static void AssertDependentChanges(IReadOnlyList<DiffLineModel> diffs, string modName, params string[] expectedDependents)
    {
        var diff = FindDiff(diffs, modName);
        Assert.NotNull(diff);
        
        var actualDependents = diff.DependentChanges.Select(d => d.FileName).ToList();
        var expectedList = expectedDependents.ToList();

        Assert.Equal(expectedList.Count, actualDependents.Count);
        
        foreach (var expected in expectedList)
        {
            Assert.Contains(expected, actualDependents);
        }
    }

    /// <summary>
    /// Asserts that no changes were detected.
    /// </summary>
    protected static void AssertNoChanges(IReadOnlyList<DiffLineModel> diffs)
    {
        Assert.Empty(diffs);
    }

    /// <summary>
    /// Asserts the total number of primary changes detected (excludes Unchanged context lines and Separators).
    /// </summary>
    protected static void AssertChangeCount(IReadOnlyList<DiffLineModel> diffs, int expectedCount)
    {
        Assert.Equal(expectedCount, diffs.Count(d =>
            d.ChangeType != DiffChangeType.Unchanged &&
            d.ChangeType != DiffChangeType.Separator));
    }

    /// <summary>
    /// Asserts that a specific mod exists in the diffs.
    /// </summary>
    protected static void AssertModExists(IReadOnlyList<DiffLineModel> diffs, string modName)
    {
        var diff = FindDiff(diffs, modName);
        Assert.NotNull(diff);
    }

    /// <summary>
    /// Asserts that a specific mod does not exist in the diffs.
    /// </summary>
    protected static void AssertModNotExists(IReadOnlyList<DiffLineModel> diffs, string modName)
    {
        var diff = diffs.FirstOrDefault(d => 
            d.FileName.Equals(modName, System.StringComparison.OrdinalIgnoreCase));
        Assert.Null(diff);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Finds a diff by mod name (case-insensitive).
    /// </summary>
    private static DiffLineModel? FindDiff(IReadOnlyList<DiffLineModel> diffs, string modName)
    {
        return diffs.FirstOrDefault(d => 
            d.FileName.Equals(modName, System.StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets the actual mod names from the plugins file.
    /// </summary>
    protected static async Task<List<string>> GetCurrentModListAsync(TestConfigContext context)
    {
        var lines = await File.ReadAllLinesAsync(context.PluginsFilePath);
        return lines
            .Where(line => !string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith("#"))
            .Select(line => line.Trim())
            .ToList();
    }

    /// <summary>
    /// Verifies that the current order matches the expected order.
    /// </summary>
    protected static async Task AssertCurrentOrderMatchesAsync(TestConfigContext context, params string[] expectedMods)
    {
        var actualMods = await GetCurrentModListAsync(context);
        Assert.Equal(expectedMods.Length, actualMods.Count);

        for (int i = 0; i < expectedMods.Length; i++)
        {
            Assert.Equal(expectedMods[i], actualMods[i], ignoreCase: false);
        }
    }

    #endregion
}
