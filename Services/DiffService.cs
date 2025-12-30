using System;
using System.Collections.Generic;
using System.IO;
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

                    string text = $"#{diff.CurrentNumber}: {displayName} added to load order";
                    result.Add(new DiffLineModel(displayName, text, DiffChangeType.Added, diff.ReferenceNumber, diff.CurrentNumber));
                }
                else if (diff.IsMoved)
                {
                    string text = $"{displayName} moved from #{diff.ReferenceNumber} to #{diff.CurrentNumber}";
                    result.Add(new DiffLineModel(displayName, text, DiffChangeType.Moved, diff.ReferenceNumber, diff.CurrentNumber));
                }
            }

            return result;
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
