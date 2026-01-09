using System.IO;
using LoadOrderKeeper.Models;

namespace LoadOrderKeeper.Services
{
    public static class DiffService
    {
        public static async Task<IReadOnlyList<DiffLineModel>> GetPluginsDiffAsync(AppConfigModel config)
        {
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (!config.IsValid())
            {
                throw new InvalidOperationException("Configuration paths are invalid.");
            }

            string targetPath = config.GetPluginsFilePath();
            string referencePath = config.GetReferenceFilePath();

            if (!File.Exists(referencePath))
            {
                throw new FileNotFoundException("Reference file not found.", referencePath);
            }

            if (!File.Exists(targetPath))
            {
                throw new FileNotFoundException("Plugins file not found.", targetPath);
            }

            var diffs = await FileService.GetModDiffAsync(config).ConfigureAwait(false);
            var result = new List<DiffLineModel>();
            var replacements = DetectReplacements(diffs, out var matchedAdditions);

            // Calculate maxReferenceNumber as the highest CURRENT position of any reference mod
            // This tells us "after which current position are mods truly 'added' vs 'inserted'"
            var referenceDiffs = diffs.Where(d => d.ReferenceNumber.HasValue).ToList();
            var existingReferenceMods = referenceDiffs.Where(d => d.CurrentNumber.HasValue).ToList();
            int maxReferenceNumber = existingReferenceMods.Any() 
                ? existingReferenceMods.Max(d => d.CurrentNumber!.Value) 
                : 0;

            foreach (var diff in diffs)
            {
                string displayName = diff.FileName;
                if (replacements.TryGetValue(diff, out var replacement))
                {
                    int? lineNumber = diff.ReferenceNumber ?? replacement.CurrentNumber;
                    string lineDescription = lineNumber is int number ? $"line {number}" : "the load order";
                    string text = $"{displayName} replaced by {replacement.FileName} in {lineDescription}";
                    result.Add(new DiffLineModel(displayName, text, DiffChangeType.Replaced, diff.ReferenceNumber, replacement.CurrentNumber, replacement.FileName));
                }
                else if (diff.IsRemoved)
                {
                    string text = $"#{diff.ReferenceNumber}: {displayName} removed from load order";
                    result.Add(new DiffLineModel(displayName, text, DiffChangeType.Removed, diff.ReferenceNumber, diff.CurrentNumber));
                }
                else if (diff.IsNew)
                {
                    if (matchedAdditions.Contains(diff))
                    {
                        continue;
                    }

                    bool isInserted = diff.CurrentNumber.HasValue && diff.CurrentNumber.Value <= maxReferenceNumber;
                    DiffChangeType changeType = isInserted ? DiffChangeType.Inserted : DiffChangeType.Added;
                    string text = $"#{diff.CurrentNumber}: {displayName} {(isInserted ? "inserted into" : "added to")} load order";
                    result.Add(new DiffLineModel(displayName, text, changeType, diff.ReferenceNumber, diff.CurrentNumber));
                }
                else if (diff.IsMoved)
                {
                    string text = $"{displayName} moved from #{diff.ReferenceNumber} to #{diff.CurrentNumber}";
                    result.Add(new DiffLineModel(displayName, text, DiffChangeType.Moved, diff.ReferenceNumber, diff.CurrentNumber));
                }
            }

            DetectAndAssignDependentChanges(result);

            return result;
        }

        /// <summary>
        /// Checks if there are any moved mods that are NOT part of dependent change lists.
        /// These independent moves indicate external reordering that sorting could fix.
        /// </summary>
        public static async Task<bool> HasIndependentMovedModsAsync(AppConfigModel config)
        {
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (!config.IsValid())
            {
                return false;
            }

            string targetPath = config.GetPluginsFilePath();
            string referencePath = config.GetReferenceFilePath();

            if (!File.Exists(referencePath) || !File.Exists(targetPath))
            {
                return false;
            }

            var diffLines = await GetPluginsDiffAsync(config).ConfigureAwait(false);
            
            // Get all mods that are part of dependent change lists
            var dependentMods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in diffLines)
            {
                foreach (var dependent in line.DependentChanges)
                {
                    dependentMods.Add(dependent.FileName);
                }
            }
            
            // Check if there are any moved mods that are NOT in the dependent set
            bool hasIndependentMoves = diffLines.Any(line => 
                line.ChangeType == DiffChangeType.Moved && 
                !dependentMods.Contains(line.FileName));
            
            return hasIndependentMoves;
        }

        private static void DetectAndAssignDependentChanges(List<DiffLineModel> allLines)
        {
            var dependentLines = new HashSet<DiffLineModel>();

            // Get all moved mods sorted by reference position
            var movedByReferencePos = allLines
                .Where(line => line.ChangeType == DiffChangeType.Moved && 
                              line.ReferenceNumber.HasValue && 
                              line.CurrentNumber.HasValue)
                .OrderBy(line => line.ReferenceNumber!.Value)
                .ToList();

            // Process removed mods (they use reference positions)
            var removedMods = allLines
                .Where(line => line.ChangeType == DiffChangeType.Removed && line.ReferenceNumber.HasValue)
                .OrderBy(line => line.ReferenceNumber!.Value)
                .ToList();

            foreach (var removed in removedMods)
            {
                int startPos = removed.ReferenceNumber!.Value + 1;

                // Find next removed mod that would stop this one's range
                var nextRemoved = removedMods
                    .Where(r => r != removed && r.ReferenceNumber!.Value >= startPos)
                    .OrderBy(r => r.ReferenceNumber!.Value)
                    .FirstOrDefault();

                int? stopBefore = nextRemoved?.ReferenceNumber;

                // Collect moved mods in this range
                foreach (var moved in movedByReferencePos)
                {
                    if (dependentLines.Contains(moved))
                        continue;

                    int refPos = moved.ReferenceNumber!.Value;
                    if (refPos < startPos)
                        continue;
                    if (stopBefore.HasValue && refPos >= stopBefore.Value)
                        break;

                    removed.DependentChanges.Add(moved);
                    dependentLines.Add(moved);
                }
            }

            // Process inserted mods (they affect mods from their insertion point onward)
            var insertedMods = allLines
                .Where(line => line.ChangeType == DiffChangeType.Inserted && line.CurrentNumber.HasValue)
                .OrderBy(line => line.CurrentNumber!.Value)
                .ToList();

            foreach (var inserted in insertedMods)
            {
                // Find the reference position where this insertion occurs
                // This is the first moved mod at or after the insertion's current position
                var firstAffected = movedByReferencePos
                    .Where(m => !dependentLines.Contains(m) && m.CurrentNumber >= inserted.CurrentNumber)
                    .OrderBy(m => m.CurrentNumber)
                    .FirstOrDefault();

                if (firstAffected == null)
                    continue;

                int startRefPos = firstAffected.ReferenceNumber!.Value;

                // Find next inserted or removed mod that would stop this range
                var nextInserted = insertedMods
                    .Where(i => i != inserted && i.CurrentNumber > inserted.CurrentNumber)
                    .OrderBy(i => i.CurrentNumber)
                    .FirstOrDefault();

                var nextRemoved = removedMods
                    .Where(r => r.ReferenceNumber >= startRefPos)
                    .OrderBy(r => r.ReferenceNumber)
                    .FirstOrDefault();

                // Use whichever comes first in reference coordinates
                int? stopBefore = null;
                if (nextRemoved != null)
                {
                    stopBefore = nextRemoved.ReferenceNumber!.Value;
                }

                // Collect moved mods starting from the affected position
                foreach (var moved in movedByReferencePos)
                {
                    if (dependentLines.Contains(moved))
                        continue;

                    int refPos = moved.ReferenceNumber!.Value;
                    if (refPos < startRefPos)
                        continue;
                    if (stopBefore.HasValue && refPos >= stopBefore.Value)
                        break;

                    inserted.DependentChanges.Add(moved);
                    dependentLines.Add(moved);
                }
            }

            // Remove all dependent changes from the main list
            allLines.RemoveAll(line => dependentLines.Contains(line));
        }

        private static Dictionary<ModDiffModel, ModDiffModel> DetectReplacements(IReadOnlyList<ModDiffModel> diffs, out HashSet<ModDiffModel> matchedAdditions)
        {
            var additionsByLine = new Dictionary<int, ModDiffModel>();
            foreach (var diff in diffs)
            {
                if (diff.IsNew && diff.CurrentNumber is int currentLine && !additionsByLine.ContainsKey(currentLine))
                {
                    additionsByLine[currentLine] = diff;
                }
            }

            var replacements = new Dictionary<ModDiffModel, ModDiffModel>();
            var usedAdditions = new HashSet<ModDiffModel>();

            foreach (var diff in diffs)
            {
                if (!diff.IsRemoved || diff.ReferenceNumber is not int referenceLine)
                {
                    continue;
                }

                if (additionsByLine.TryGetValue(referenceLine, out var candidate) && usedAdditions.Add(candidate))
                {
                    replacements[diff] = candidate;
                }
            }

            matchedAdditions = usedAdditions;
            return replacements;
        }
    }
}
