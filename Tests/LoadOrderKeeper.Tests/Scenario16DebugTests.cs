using System.Linq;
using System.Threading.Tasks;
using LoadOrderKeeper.Models;
using LoadOrderKeeper.Services;
using Xunit;
using Xunit.Abstractions;

namespace LoadOrderKeeper.Tests;

/// <summary>
/// Temporary debug test for Scenario 16 to understand the ModDiffModel data
/// </summary>
public class Scenario16DebugTests : ScenarioTestBase
{
    private readonly ITestOutputHelper _output;

    public Scenario16DebugTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task DebugScenario16_ModDiffData()
    {
        // Arrange
        using var context = new TestConfigContext();
        await SetupStandardReferenceAsync(context);
        
        var modifiedOrder = new[]
        {
            "*StarfieldCommunityPatch.esm",
            "*AmazonCrew.esm",
            "*ShipBuilderCategories.esm",
            "*BetterShipPartFlips.esm",
            "*Better_Living.esm",           // Position 5 (shifted from 6)
            "*Richer Merchants.esm",        // Position 6 (shifted from 7)
            "*xatmosPerkUpVendors.esp",     // Position 7 (shifted from 8)
            "*fixgraydockingcolors.esm",    // Position 8 (shifted from 10)
            "*DayLengthMessage.esm",        // Position 9 (shifted from 11)
            "*Eit_Clothiers_Z.esm",         // Position 10 (shifted from 12)
            "*Easy Digipick.esm",           // Position 11 (shifted from 13)
            "*Eli_RenamedSnowglobes.esm",   // Position 12 (shifted from 14)
            "*Nanosuit_f_new.esm",          // Position 13 (shifted from 15)
            "*OutpostFishTank.esm",         // Position 14 (shifted from 16)
            "*Fragile2.esm",                // Position 15 (should replace Fragile.esm at shifted position)
            "*GagarinNewDawn.esm"           // Position 16 (shifted from 18)
        };
        await SetupCurrentOrderAsync(context, modifiedOrder);

        // Act - Get raw ModDiffModels
        var modDiffs = await FileService.GetModDiffAsync(context.Config);
        
        // Output all diffs
        _output.WriteLine("=== ALL MOD DIFFS ===");
        foreach (var diff in modDiffs)
        {
            _output.WriteLine($"{diff.FileName,-40} Ref: {diff.ReferenceNumber,4} Cur: {diff.CurrentNumber,4} | New: {diff.IsNew} Removed: {diff.IsRemoved} Moved: {diff.IsMoved}");
        }
        
        // Find specific mods
        _output.WriteLine("\n=== KEY MODS ===");
        var fragile = modDiffs.FirstOrDefault(d => d.FileName.Equals("Fragile.esm", System.StringComparison.OrdinalIgnoreCase));
        var fragile2 = modDiffs.FirstOrDefault(d => d.FileName.Equals("Fragile2.esm", System.StringComparison.OrdinalIgnoreCase));
        var snaps = modDiffs.FirstOrDefault(d => d.FileName.Equals("BetterShipPartSnaps.esm", System.StringComparison.OrdinalIgnoreCase));
        var buySwimsuits = modDiffs.FirstOrDefault(d => d.FileName.Equals("BuySwimsuits.esm", System.StringComparison.OrdinalIgnoreCase));
        
        _output.WriteLine($"Fragile.esm: Ref={fragile?.ReferenceNumber} Cur={fragile?.CurrentNumber} IsRemoved={fragile?.IsRemoved}");
        _output.WriteLine($"Fragile2.esm: Ref={fragile2?.ReferenceNumber} Cur={fragile2?.CurrentNumber} IsNew={fragile2?.IsNew}");
        _output.WriteLine($"BetterShipPartSnaps.esm: Ref={snaps?.ReferenceNumber} Cur={snaps?.CurrentNumber} IsRemoved={snaps?.IsRemoved}");
        _output.WriteLine($"BuySwimsuits.esm: Ref={buySwimsuits?.ReferenceNumber} Cur={buySwimsuits?.CurrentNumber} IsRemoved={buySwimsuits?.IsRemoved}");
        
        // Calculate expected shift
        var removedMods = modDiffs.Where(d => d.IsRemoved && d.ReferenceNumber.HasValue).OrderBy(d => d.ReferenceNumber).ToList();
        _output.WriteLine($"\n=== REMOVED MODS (count: {removedMods.Count}) ===");
        foreach (var removed in removedMods)
        {
            _output.WriteLine($"{removed.FileName} at position {removed.ReferenceNumber}");
        }
        
        if (fragile != null)
        {
            int deletionsBeforeFragile = removedMods.Count(r => r.ReferenceNumber < fragile.ReferenceNumber);
            int expectedShiftedPos = fragile.ReferenceNumber!.Value - deletionsBeforeFragile;
            _output.WriteLine($"\nFragile.esm shift calculation:");
            _output.WriteLine($"  Reference position: {fragile.ReferenceNumber}");
            _output.WriteLine($"  Deletions before: {deletionsBeforeFragile}");
            _output.WriteLine($"  Expected shifted position: {expectedShiftedPos}");
            _output.WriteLine($"  Fragile2.esm actual position: {fragile2?.CurrentNumber}");
            _output.WriteLine($"  Match: {fragile2?.CurrentNumber == expectedShiftedPos}");
        }
    }

    [Fact]
    public async Task DebugScenario16_CheckReplacementsDictionary()
    {
        // Arrange
        using var context = new TestConfigContext();
        await SetupStandardReferenceAsync(context);
        
        var modifiedOrder = new[]
        {
            "*StarfieldCommunityPatch.esm",
            "*AmazonCrew.esm",
            "*ShipBuilderCategories.esm",
            "*BetterShipPartFlips.esm",
            "*Better_Living.esm",
            "*Richer Merchants.esm",
            "*xatmosPerkUpVendors.esp",
            "*fixgraydockingcolors.esm",
            "*DayLengthMessage.esm",
            "*Eit_Clothiers_Z.esm",
            "*Easy Digipick.esm",
            "*Eli_RenamedSnowglobes.esm",
            "*Nanosuit_f_new.esm",
            "*OutpostFishTank.esm",
            "*Fragile2.esm",                // Position 15
            "*GagarinNewDawn.esm"
        };
        await SetupCurrentOrderAsync(context, modifiedOrder);

        // Act - Get the processed diff lines
        var diffLines = await DiffService.GetPluginsDiffAsync(context.Config);
        
        // Output
        _output.WriteLine("=== DIFF LINES ===");
        foreach (var line in diffLines)
        {
            _output.WriteLine($"{line.ChangeType,-12} {line.FileName,-40} Ref: {line.ReferenceNumber,4} Cur: {line.CurrentNumber,4}");
        }
        
        _output.WriteLine($"\n=== TOTAL CHANGES: {diffLines.Count} ===");
        
        // Check Fragile specifically
        var fragile = diffLines.FirstOrDefault(d => d.FileName.Equals("Fragile.esm", System.StringComparison.OrdinalIgnoreCase));
        var fragile2 = diffLines.FirstOrDefault(d => d.FileName.Equals("Fragile2.esm", System.StringComparison.OrdinalIgnoreCase));
        
        _output.WriteLine($"\n=== FRAGILE MODS ===");
        _output.WriteLine($"Fragile.esm found: {fragile != null}, ChangeType: {fragile?.ChangeType}");
        _output.WriteLine($"Fragile2.esm found: {fragile2 != null}, ChangeType: {fragile2?.ChangeType}");
    }

    [Fact]
    public async Task DebugScenario16_FullTestData()
    {
        // Arrange - USE EXACT DATA FROM SCENARIO 16
        using var context = new TestConfigContext();
        await SetupStandardReferenceAsync(context);
        
        var modifiedOrder = new[]
        {
            "*StarfieldCommunityPatch.esm",
            "*AmazonCrew.esm",
            "*ShipBuilderCategories.esm",
            "*BetterShipPartFlips.esm",
            "*Better_Living.esm",           // Position 5 (shifted from 6)
            "*Richer Merchants.esm",        // Position 6 (shifted from 7)
            "*xatmosPerkUpVendors.esp",     // Position 7 (shifted from 8)
            "*fixgraydockingcolors.esm",    // Position 8 (shifted from 10)
            "*DayLengthMessage.esm",        // Position 9 (shifted from 11)
            "*Eit_Clothiers_Z.esm",         // Position 10 (shifted from 12)
            "*Easy Digipick.esm",           // Position 11 (shifted from 13)
            "*Eli_RenamedSnowglobes.esm",   // Position 12 (shifted from 14)
            "*Nanosuit_f_new.esm",          // Position 13 (shifted from 15)
            "*OutpostFishTank.esm",         // Position 14 (shifted from 16)
            "*Fragile2.esm",                // Position 15 (replaces Fragile.esm at shifted position)
            "*GagarinNewDawn.esm"           // Position 16 (shifted from 18)
        };
        await SetupCurrentOrderAsync(context, modifiedOrder);

        // Act
        var diffLines = await DiffService.GetPluginsDiffAsync(context.Config);
        
        // Output ALL changes
        _output.WriteLine($"=== ALL DIFF LINES (Total: {diffLines.Count}) ===");
        foreach (var line in diffLines)
        {
            _output.WriteLine($"{line.ChangeType,-12} {line.FileName,-40} Ref: {line.ReferenceNumber,4} Cur: {line.CurrentNumber,4} Deps: {line.DependentChanges.Count}");
        }
    }
}
