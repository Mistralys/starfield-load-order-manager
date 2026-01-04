using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

            var referenceDiffs = diffs.Where(d => d.ReferenceNumber.HasValue).ToList();
            int maxReferenceNumber = referenceDiffs.Any() ? referenceDiffs.Max(d => d.ReferenceNumber!.Value) : 0;

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

        private static void DetectAndAssignDependentChanges(List<DiffLineModel> allLines)
        {
            foreach (var line in allLines)
            {
                if (line.ChangeType != DiffChangeType.Removed && line.ChangeType != DiffChangeType.Inserted)
                {
                    continue;
                }

                var primaryPosition = line.ChangeType == DiffChangeType.Removed 
                    ? line.ReferenceNumber 
                    : line.CurrentNumber;

                if (!primaryPosition.HasValue)
                {
                    continue;
                }

                var dependents = new List<DiffLineModel>();

                foreach (var potentialDependent in allLines)
                {
                    if (potentialDependent.ChangeType != DiffChangeType.Moved)
                    {
                        continue;
                    }

                    if (!potentialDependent.ReferenceNumber.HasValue || !potentialDependent.CurrentNumber.HasValue)
                    {
                        continue;
                    }

                    bool isAffected = line.ChangeType == DiffChangeType.Removed
                        ? potentialDependent.ReferenceNumber.Value > primaryPosition.Value
                        : potentialDependent.ReferenceNumber.Value >= primaryPosition.Value;

                    if (isAffected)
                    {
                        dependents.Add(potentialDependent);
                    }
                }

                foreach (var dependent in dependents.OrderBy(d => d.CurrentNumber))
                {
                    line.DependentChanges.Add(dependent);
                }
            }
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
