using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LoadOrderKeeper.Models;
using LoadOrderKeeper.Services;
using Xunit;
using Xunit.Abstractions;

namespace LoadOrderKeeper.Tests;

/// <summary>
/// Diagnostic tests for replacement detection algorithm with detailed logging.
/// These tests provide step-by-step insight into how the two-pass replacement
/// detection algorithm processes position shifts caused by earlier deletions.
/// </summary>
[Trait("Category", "Diagnostic")]
public class ReplacementDetectionDiagnostics : ScenarioTestBase
{
    private readonly ITestOutputHelper _output;

    public ReplacementDetectionDiagnostics(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Comprehensive diagnostic test that logs every step of the replacement detection algorithm.
    /// This test is valuable for:
    /// - Verifying the algorithm works correctly
    /// - Diagnosing future regressions
    /// - Understanding how position shifts are calculated
    /// - Documenting algorithm behavior with real data
    /// 
    /// Scenario: Two mods deleted (positions 5, 9), one mod replaced at shifted position (17 ? 15)
    /// Expected: Fragile.esm at ref pos 17 replaced by Fragile2.esm at current pos 15
    /// Calculation: 17 - 2 deletions = 15 ?
    /// </summary>
    [Fact]
    public async Task DetailedReplacementDetection_WithPositionShifts()
    {
        // Arrange - Scenario 16 test data
        using var context = new TestConfigContext();
        await SetupStandardReferenceAsync(context);
        
        var modifiedOrder = new[]
        {
            "*StarfieldCommunityPatch.esm",
            "*AmazonCrew.esm",
            "*ShipBuilderCategories.esm",
            "*BetterShipPartFlips.esm",
            "*Better_Living.esm",           // Position 5 (shifted from 6 due to deletion at 5)
            "*Richer Merchants.esm",        // Position 6 (shifted from 7)
            "*xatmosPerkUpVendors.esp",     // Position 7 (shifted from 8)
            "*fixgraydockingcolors.esm",    // Position 8 (shifted from 10 due to deletion at 9)
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

        // Act - Get raw mod diffs
        var rawDiffs = await FileService.GetModDiffAsync(context.Config);
        
        _output.WriteLine("=== RAW MOD DIFFS ===");
        foreach (var diff in rawDiffs)
        {
            _output.WriteLine($"{diff.FileName,-40} Ref: {diff.ReferenceNumber,4} Cur: {diff.CurrentNumber,4} | IsNew: {diff.IsNew}, IsRemoved: {diff.IsRemoved}");
        }
        
        // Manually simulate DetectReplacements logic
        _output.WriteLine("\n=== SIMULATING DETECT REPLACEMENTS ALGORITHM ===");
        
        // Build additionsByLine dictionary
        var additionsByLine = new Dictionary<int, ModDiffModel>();
        foreach (var diff in rawDiffs)
        {
            if (diff.IsNew && diff.CurrentNumber is int currentLine)
            {
                _output.WriteLine($"Adding to dictionary: [{currentLine}] = {diff.FileName}");
                if (!additionsByLine.ContainsKey(currentLine))
                {
                    additionsByLine[currentLine] = diff;
                }
            }
        }
        
        _output.WriteLine($"\nadditionsByLine dictionary has {additionsByLine.Count} entries");
        foreach (var kvp in additionsByLine.OrderBy(k => k.Key))
        {
            _output.WriteLine($"  [{kvp.Key}] = {kvp.Value.FileName}");
        }
        
        // Get removed mods
        var removedMods = rawDiffs
            .Where(d => d.IsRemoved && d.ReferenceNumber.HasValue)
            .OrderBy(d => d.ReferenceNumber!.Value)
            .ToList();
        
        _output.WriteLine($"\nRemoved mods: {removedMods.Count}");
        foreach (var removed in removedMods)
        {
            _output.WriteLine($"  {removed.FileName,-40} at reference position {removed.ReferenceNumber}");
        }
        
        // First pass: Exact position matching
        _output.WriteLine("\n=== FIRST PASS: EXACT POSITION MATCHING ===");
        var replacements = new Dictionary<string, string>();
        var usedAdditions = new HashSet<string>();
        
        foreach (var removed in removedMods)
        {
            int refPos = removed.ReferenceNumber!.Value;
            _output.WriteLine($"Checking {removed.FileName} at ref pos {refPos}...");
            
            if (additionsByLine.TryGetValue(refPos, out var candidate))
            {
                _output.WriteLine($"  MATCH FOUND: {candidate.FileName} at position {refPos}");
                replacements[removed.FileName] = candidate.FileName;
                usedAdditions.Add(candidate.FileName);
            }
            else
            {
                _output.WriteLine($"  No match at position {refPos}");
            }
        }
        
        // Second pass: Shifted position matching (THE ENHANCEMENT)
        _output.WriteLine("\n=== SECOND PASS: SHIFTED POSITION MATCHING (ENHANCEMENT) ===");
        
        foreach (var removed in removedMods)
        {
            if (replacements.ContainsKey(removed.FileName))
            {
                _output.WriteLine($"Skipping {removed.FileName} (already matched in first pass)");
                continue;
            }
            
            int referencePosition = removed.ReferenceNumber!.Value;
            int deletionsBeforeThisPosition = removedMods.Count(r => r.ReferenceNumber!.Value < referencePosition);
            int shiftedPosition = referencePosition - deletionsBeforeThisPosition;
            
            _output.WriteLine($"Checking {removed.FileName}:");
            _output.WriteLine($"  Reference position: {referencePosition}");
            _output.WriteLine($"  Deletions before: {deletionsBeforeThisPosition}");
            _output.WriteLine($"  Shifted position: {shiftedPosition}");
            
            if (additionsByLine.TryGetValue(shiftedPosition, out var candidate))
            {
                if (!usedAdditions.Contains(candidate.FileName))
                {
                    _output.WriteLine($"  ? MATCH FOUND: {candidate.FileName} at shifted position {shiftedPosition}");
                    replacements[removed.FileName] = candidate.FileName;
                    usedAdditions.Add(candidate.FileName);
                }
                else
                {
                    _output.WriteLine($"  ? Found {candidate.FileName} but already used");
                }
            }
            else
            {
                _output.WriteLine($"  ? No match at shifted position {shiftedPosition}");
                _output.WriteLine($"  Dictionary keys: {string.Join(", ", additionsByLine.Keys.OrderBy(k => k))}");
            }
        }
        
        _output.WriteLine($"\n=== FINAL REPLACEMENTS: {replacements.Count} ===");
        foreach (var kvp in replacements)
        {
            _output.WriteLine($"  {kvp.Key} ? {kvp.Value}");
        }
        
        // Get actual result from DiffService
        _output.WriteLine("\n=== ACTUAL DIFF SERVICE RESULT ===");
        var actualDiffs = await DiffService.GetPluginsDiffAsync(context.Config);
        foreach (var diff in actualDiffs)
        {
            _output.WriteLine($"{diff.ChangeType,-12} {diff.FileName}");
        }
        
        // Verify Fragile was detected as replaced
        var fragile = actualDiffs.FirstOrDefault(d => d.FileName.Equals("Fragile.esm", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(fragile);
        
        _output.WriteLine($"\n=== VERIFICATION ===");
        _output.WriteLine($"Fragile.esm ChangeType: {fragile.ChangeType}");
        
        if (fragile.ChangeType == DiffChangeType.Replaced)
        {
            _output.WriteLine("? SUCCESS: Replacement detected correctly!");
        }
        else
        {
            _output.WriteLine("? FAILURE: Replacement not detected!");
            _output.WriteLine("This indicates a regression in the algorithm.");
        }
        
        // Assert the algorithm worked
        Assert.Equal(DiffChangeType.Replaced, fragile.ChangeType);
    }
}
