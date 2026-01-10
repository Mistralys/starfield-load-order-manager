using System.Collections.Generic;
using System.Linq;
using LoadOrderKeeper.Models;
using Xunit;
using Xunit.Abstractions;

namespace LoadOrderKeeper.Tests;

/// <summary>
/// Direct test of DetectReplacements logic
/// </summary>
public class DetectReplacementsTests
{
    private readonly ITestOutputHelper _output;

    public DetectReplacementsTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void DetectReplacements_WithPositionShift_FindsMatch()
    {
        // Arrange - simulate the Scenario 16 data
        var diffs = new List<ModDiffModel>
        {
            // Removed mods
            new ModDiffModel { FileName = "BetterShipPartSnaps.esm", ReferenceNumber = 5, CurrentNumber = null },
            new ModDiffModel { FileName = "BuySwimsuits.esm", ReferenceNumber = 9, CurrentNumber = null },
            new ModDiffModel { FileName = "Fragile.esm", ReferenceNumber = 17, CurrentNumber = null },
            
            // New mod (replacement)
            new ModDiffModel { FileName = "Fragile2.esm", ReferenceNumber = null, CurrentNumber = 15 },
            
            // Some moved mods for context
            new ModDiffModel { FileName = "Better_Living.esm", ReferenceNumber = 6, CurrentNumber = 5 },
            new ModDiffModel { FileName = "OutpostFishTank.esm", ReferenceNumber = 16, CurrentNumber = 14 }
        };

        // Act - call DetectReplacements using reflection
        var method = typeof(LoadOrderKeeper.Services.DiffService)
            .GetMethod("DetectReplacements", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        var parameters = new object[] { diffs, null };
        var replacements = (Dictionary<ModDiffModel, ModDiffModel>)method.Invoke(null, parameters);
        var matchedAdditions = (HashSet<ModDiffModel>)parameters[1];

        // Assert
        _output.WriteLine($"Replacements found: {replacements.Count}");
        foreach (var kvp in replacements)
        {
            _output.WriteLine($"  {kvp.Key.FileName} (ref #{kvp.Key.ReferenceNumber}) -> {kvp.Value.FileName} (cur #{kvp.Value.CurrentNumber})");
        }
        
        _output.WriteLine($"\nMatched additions: {matchedAdditions.Count}");
        foreach (var addition in matchedAdditions)
        {
            _output.WriteLine($"  {addition.FileName} at position {addition.CurrentNumber}");
        }

        // Verify Fragile.esm -> Fragile2.esm replacement was found
        var fragileRemoved = diffs.First(d => d.FileName == "Fragile.esm");
        Assert.True(replacements.ContainsKey(fragileRemoved), "Fragile.esm should be in replacements dictionary");
        
        var replacement = replacements[fragileRemoved];
        Assert.Equal("Fragile2.esm", replacement.FileName);
        Assert.Equal(15, replacement.CurrentNumber);
    }
}
