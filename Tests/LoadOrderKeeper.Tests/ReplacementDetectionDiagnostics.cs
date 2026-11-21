using System;
using System.Linq;
using System.Threading.Tasks;
using LoadOrderKeeper.Models;
using LoadOrderKeeper.Services;
using Xunit;
using Xunit.Abstractions;

namespace LoadOrderKeeper.Tests;

/// <summary>
/// Behavioral regression diagnostics for Scenario 16: replacement detection under position shift.
///
/// This file serves as a diagnostic companion to ScenarioTests.Scenario_16. Its primary purpose
/// is to exercise the <see cref="DiffService.GetPluginsDiffAsync"/> output for a complex scenario
/// and emit a step-by-step diagnostic log that aids debugging if a regression occurs.
///
/// <para>
/// The production pipeline uses an LCS (Longest Common Subsequence) approach:
/// <c>FileService.ReadModListAsync</c> → <c>DiffService.ComputeLcs</c> → <c>DiffService.ClassifyChanges</c>.
/// Position shifts are handled inherently by LCS alignment rather than a post-hoc shift correction.
/// The behavioral assertion at the end of <see cref="DetailedReplacementDetection_WithPositionShifts"/>
/// is the authoritative regression check.
/// </para>
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
    /// Diagnostic test that logs the Scenario 16 diff result for debugging regressions in
    /// replacement-under-position-shift detection.
    ///
    /// <para>
    /// <b>Scenario:</b> Two mods deleted (reference positions 5 and 9); Fragile.esm (reference
    /// position 17) replaced by Fragile2.esm. Because of the two earlier deletions, Fragile2.esm
    /// appears at current position 15 (17 − 2 = 15). The LCS pipeline aligns these correctly
    /// without any explicit shift correction.
    /// </para>
    ///
    /// <para>
    /// <b>Authoritative assertion:</b> <c>Assert.Equal(DiffChangeType.Replaced, fragile.ChangeType)</c>
    /// verifies that the LCS pipeline correctly classifies Fragile.esm as <c>Replaced</c>. A failure
    /// here is a confirmed regression in <c>DiffService.ClassifyChanges</c> Step 3 (replacement
    /// detection) or <c>ComputeLcs</c>.
    /// </para>
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

        // -----------------------------------------------------------------------
        // Act — LCS pipeline (production path)
        // GetPluginsDiffAsync calls ReadModListAsync → ComputeLcs → ClassifyChanges.
        // -----------------------------------------------------------------------
        _output.WriteLine("\n=== ACTUAL DIFF SERVICE RESULT (LCS pipeline) ===");
        var actualDiffs = await DiffService.GetPluginsDiffAsync(context.Config);
        foreach (var diff in actualDiffs)
        {
            _output.WriteLine($"{diff.ChangeType,-12} {diff.FileName}");
        }
        
        // Verify Fragile was detected as replaced
        var fragile = actualDiffs.FirstOrDefault(d => d.FileName.Equals("Fragile.esm", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(fragile); // Fragile.esm should appear in the diff — if null, check Scenario 16 test data setup
        
        _output.WriteLine($"\n=== VERIFICATION ===");
        _output.WriteLine($"Fragile.esm ChangeType: {fragile.ChangeType}");
        
        if (fragile.ChangeType == DiffChangeType.Replaced)
        {
            _output.WriteLine("SUCCESS: Replacement detected correctly by LCS pipeline!");
        }
        else
        {
            _output.WriteLine("FAILURE: Replacement not detected!");
            _output.WriteLine("This indicates a regression in DiffService.ClassifyChanges Step 3 (replacement detection)");
            _output.WriteLine("or in ComputeLcs. Review the LCS alignment for Fragile.esm vs Fragile2.esm.");
        }
        
        // Assert the algorithm worked
        Assert.Equal(DiffChangeType.Replaced, fragile.ChangeType);
    }
}
