using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LoadOrderKeeper.Models;

namespace LoadOrderKeeper.Services
{
    public static class FileService
    {
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

        private static Dictionary<string, string> GetCaseLookup(string gamePath)
        {
            string dataPath = Path.Combine(gamePath, "Data");
            if (!Directory.Exists(dataPath))
            {
                return new Dictionary<string, string>();
            }

            var files = Directory.EnumerateFiles(dataPath, "*.esm", SearchOption.AllDirectories)
                .Concat(Directory.EnumerateFiles(dataPath, "*.esp", SearchOption.AllDirectories));

            return files.ToDictionary(
                p => Path.GetFileName(p).ToLowerInvariant(),
                p => Path.GetFileName(p));
        }

        private static async Task<List<ModEntryModel>> ReadFileAsync(string filePath, bool isReferenceFile = false)
        {
            var result = new List<ModEntryModel>();
            if (!File.Exists(filePath))
            {
                return result;
            }

            var lines = await File.ReadAllLinesAsync(filePath, Encoding.UTF8);
            int logicalIndex = 0;
            foreach (var line in lines)
            {
                if (!TryParseEnabledModEntry(line, out var entry))
                {
                    continue;
                }

                logicalIndex++;
                entry.LineNumber = logicalIndex;
                if (isReferenceFile)
                {
                    entry.OriginalLineNumber = logicalIndex;
                }

                result.Add(entry);
            }

            return result;
        }

        public static bool DoesReferenceFileExist(AppConfigModel config)
        {
            return File.Exists(config.GetReferenceFilePath());
        }

        public static async Task CreateReferenceFileAsync(AppConfigModel config)
        {
            string targetPath = config.GetPluginsFilePath();
            string referencePath = config.GetReferenceFilePath();

            if (!File.Exists(targetPath))
            {
                throw new FileNotFoundException($"Target file not found: {targetPath}");
            }

            // Ensure the profile folder exists
            var referenceDir = Path.GetDirectoryName(referencePath);
            if (!string.IsNullOrEmpty(referenceDir) && !Directory.Exists(referenceDir))
            {
                Directory.CreateDirectory(referenceDir);
            }

            string content = await File.ReadAllTextAsync(targetPath, Encoding.UTF8);
            await File.WriteAllTextAsync(referencePath, content, Utf8NoBom);
        }

        public static async Task ApplyLoadOrderAsync(AppConfigModel config)
        {
            if (!config.IsValid())
            {
                throw new InvalidOperationException("Configuration paths are invalid.");
            }

            var (referenceMods, currentMods, targetPath) = await LoadModStateAsync(config);
            await WriteAlignedLoadOrderAsync(config, referenceMods, currentMods, targetPath);
        }

        public static async Task<bool> HasPluginsFileChangedAsync(AppConfigModel config)
        {
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

            var referenceLines = await File.ReadAllLinesAsync(referencePath, Encoding.UTF8);
            var targetLines = await File.ReadAllLinesAsync(targetPath, Encoding.UTF8);

            return !SequencesEqualIgnoringTrailingEmpty(referenceLines, targetLines);
        }

        public static async Task<PluginsComparisonResult> ComparePluginsWithReferenceAsync(AppConfigModel config)
        {
            if (!config.IsValid())
            {
                return new PluginsComparisonResult(false, string.Empty);
            }

            string targetPath = config.GetPluginsFilePath();
            string referencePath = config.GetReferenceFilePath();

            if (!File.Exists(referencePath) || !File.Exists(targetPath))
            {
                return new PluginsComparisonResult(false, string.Empty);
            }

            var referenceLines = await File.ReadAllLinesAsync(referencePath, Encoding.UTF8);
            var targetLines = await File.ReadAllLinesAsync(targetPath, Encoding.UTF8);

            bool hasDifferences = !SequencesEqualIgnoringTrailingEmpty(referenceLines, targetLines);
            string signature = BuildPluginsSignature(targetLines);

            return new PluginsComparisonResult(hasDifferences, signature);
        }

        public static async Task<IReadOnlyList<ModDiffModel>> GetModDiffAsync(AppConfigModel config)
        {
            return await GetModDiffInternalAsync(config, alignCurrentToReference: false);
        }

        private static async Task<IReadOnlyList<ModDiffModel>> GetModDiffInternalAsync(AppConfigModel config, bool alignCurrentToReference)
        {
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

            var referenceMods = await ReadFileAsync(referencePath, true);
            var currentMods = await ReadFileAsync(targetPath);

            if (alignCurrentToReference)
            {
                AlignCurrentModsWithReference(referenceMods, currentMods);
            }

            var referenceLookup = referenceMods.ToDictionary(m => m.FileName, StringComparer.OrdinalIgnoreCase);
            var currentLookup = currentMods.ToDictionary(m => m.FileName, StringComparer.OrdinalIgnoreCase);

            var orderedNames = referenceLookup.Keys
                .Union(currentLookup.Keys, StringComparer.OrdinalIgnoreCase)
                .Select(name => new
                {
                    Name = name,
                    ReferenceNumber = referenceLookup.TryGetValue(name, out var refMod) ? refMod.LineNumber : (int?)null,
                    CurrentNumber = currentLookup.TryGetValue(name, out var curMod) ? curMod.LineNumber : (int?)null
                })
                .OrderBy(x => x.ReferenceNumber ?? int.MaxValue)
                .ThenBy(x => x.CurrentNumber ?? int.MaxValue)
                .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var diffs = new List<ModDiffModel>(orderedNames.Count);

            foreach (var entry in orderedNames)
            {
                diffs.Add(new ModDiffModel
                {
                    FileName = entry.Name,
                    ReferenceNumber = entry.ReferenceNumber,
                    CurrentNumber = entry.CurrentNumber
                });
            }

            return diffs;
        }

        public static async Task<bool> WouldSortingChangeDiffsAsync(AppConfigModel config)
        {
            var currentDiffs = await GetModDiffInternalAsync(config, alignCurrentToReference: false);
            var sortedDiffs = await GetModDiffInternalAsync(config, alignCurrentToReference: true);
            return !DiffSequencesEqual(currentDiffs, sortedDiffs);
        }

        private static void AlignCurrentModsWithReference(IReadOnlyList<ModEntryModel> referenceMods, List<ModEntryModel> currentMods)
        {
            if (referenceMods.Count == 0 || currentMods.Count == 0)
            {
                return;
            }

            var currentLookup = currentMods.ToDictionary(m => m.FileName, StringComparer.OrdinalIgnoreCase);
            var assigned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int normalizedIndex = 0;

            foreach (var referenceMod in referenceMods)
            {
                if (currentLookup.TryGetValue(referenceMod.FileName, out var currentMod))
                {
                    normalizedIndex++;
                    currentMod.LineNumber = normalizedIndex;
                    assigned.Add(referenceMod.FileName);
                }
            }

            foreach (var mod in currentMods)
            {
                if (!assigned.Contains(mod.FileName))
                {
                    normalizedIndex++;
                    mod.LineNumber = normalizedIndex;
                }
            }
        }

        private static bool DiffSequencesEqual(IReadOnlyList<ModDiffModel> first, IReadOnlyList<ModDiffModel> second)
        {
            if (first.Count != second.Count)
            {
                return false;
            }

            for (int i = 0; i < first.Count; i++)
            {
                var left = first[i];
                var right = second[i];
                if (!string.Equals(left.FileName, right.FileName, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (left.ReferenceNumber != right.ReferenceNumber || left.CurrentNumber != right.CurrentNumber)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool SequencesEqualIgnoringTrailingEmpty(string[] first, string[] second)
        {
            var normalizedFirst = TrimTrailingEmptyLines(first);
            var normalizedSecond = TrimTrailingEmptyLines(second);

            if (normalizedFirst.Count != normalizedSecond.Count)
            {
                return false;
            }

            for (int i = 0; i < normalizedFirst.Count; i++)
            {
                if (!string.Equals(normalizedFirst[i], normalizedSecond[i], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private static IReadOnlyList<string> TrimTrailingEmptyLines(string[] lines)
        {
            int lastIndex = lines.Length - 1;
            while (lastIndex >= 0 && string.IsNullOrWhiteSpace(lines[lastIndex]))
            {
                lastIndex--;
            }

            if (lastIndex == lines.Length - 1)
            {
                return lines;
            }

            var result = new string[lastIndex + 1];
            Array.Copy(lines, result, lastIndex + 1);
            return result;
        }

        private static string FormatLine(ModEntryModel mod, Dictionary<string, string> caseLookup)
        {
            if (!mod.IsEnabled)
            {
                throw new InvalidOperationException("Cannot format a disabled mod entry.");
            }

            var cleanFileName = mod.FileName.ToLowerInvariant();
            var resolvedName = caseLookup.TryGetValue(cleanFileName, out var correctCase)
                ? correctCase
                : mod.FileName;

            return $"*{resolvedName}";
        }

        private static bool TryParseEnabledModEntry(string line, out ModEntryModel entry)
        {
            entry = default!;
            if (string.IsNullOrWhiteSpace(line))
            {
                return false;
            }

            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("#", StringComparison.Ordinal))
            {
                return false;
            }

            if (!trimmed.StartsWith("*", StringComparison.Ordinal))
            {
                return false;
            }

            var candidate = new ModEntryModel(line);
            if (!candidate.IsEnabled)
            {
                return false;
            }

            entry = candidate;
            return true;
        }

        public static async Task DiscardChangesAsync(AppConfigModel config)
        {
            if (!config.IsValid())
            {
                throw new InvalidOperationException("Configuration paths are invalid.");
            }

            string referencePath = config.GetReferenceFilePath();
            string pluginsPath = config.GetPluginsFilePath();

            if (!File.Exists(referencePath))
            {
                throw new FileNotFoundException("Reference file not found.", referencePath);
            }

            if (!File.Exists(pluginsPath))
            {
                throw new FileNotFoundException("Plugins file not found.", pluginsPath);
            }

            string content = await File.ReadAllTextAsync(referencePath, Encoding.UTF8);
            await File.WriteAllTextAsync(pluginsPath, content, Encoding.UTF8);
        }

        private static string BuildPluginsSignature(string[] lines)
        {
            var normalized = TrimTrailingEmptyLines(lines);
            var builder = new StringBuilder();
            foreach (var line in normalized)
            {
                builder.AppendLine(line);
            }

            return builder.ToString();
        }

        private static async Task<(List<ModEntryModel> referenceMods, List<ModEntryModel> currentMods, string targetPath)> LoadModStateAsync(AppConfigModel config)
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

            var referenceMods = await ReadFileAsync(referencePath, isReferenceFile: true).ConfigureAwait(false);
            var currentMods = await ReadFileAsync(targetPath).ConfigureAwait(false);
            return (referenceMods, currentMods, targetPath);
        }

        private static async Task WriteAlignedLoadOrderAsync(AppConfigModel config, List<ModEntryModel> referenceMods, List<ModEntryModel> currentMods, string targetPath)
        {
            var caseLookup = GetCaseLookup(config.StarfieldGamePath);
            var currentModSet = new HashSet<ModEntryModel>(currentMods);
            var currentModLookup = currentMods.ToDictionary(m => m.FileName, StringComparer.OrdinalIgnoreCase);
            var referenceModLookup = referenceMods.ToDictionary(m => m.FileName, StringComparer.OrdinalIgnoreCase);
            
            // Detect replacements: new mods at the same position as removed reference mods
            var replacements = new Dictionary<int, ModEntryModel>();
            var processedNewMods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var referenceMod in referenceMods)
            {
                // Skip if this reference mod still exists in current
                if (currentModLookup.ContainsKey(referenceMod.FileName))
                {
                    continue;
                }
                
                // This reference mod was removed - check if a new mod took its position
                int referencePosition = referenceMod.LineNumber!.Value;
                
                // Find a new mod at this exact position
                var replacementCandidate = currentMods.FirstOrDefault(cm => 
                    cm.LineNumber == referencePosition && 
                    !referenceModLookup.ContainsKey(cm.FileName) &&
                    !processedNewMods.Contains(cm.FileName));
                
                if (replacementCandidate != null)
                {
                    replacements[referencePosition] = replacementCandidate;
                    processedNewMods.Add(replacementCandidate.FileName);
                }
            }
            
            // Build final order
            var finalOrder = new List<string>(referenceMods.Count + currentMods.Count);
            
            // Process reference mods in order, preserving replacements
            foreach (var referenceMod in referenceMods)
            {
                if (!referenceMod.IsEnabled)
                {
                    continue;
                }
                
                int position = referenceMod.LineNumber!.Value;
                
                // Check if this position has a replacement
                if (replacements.TryGetValue(position, out var replacementMod))
                {
                    // Use the replacement mod instead
                    finalOrder.Add(FormatLine(replacementMod, caseLookup));
                }
                else if (currentModSet.Contains(referenceMod))
                {
                    // Reference mod still exists, keep it
                    finalOrder.Add(FormatLine(referenceMod, caseLookup));
                }
                // If neither, the mod was removed - skip it
            }
            
            // Add remaining new mods that weren't replacements (append to end)
            var newMods = currentMods.Where(mod => 
                !referenceMods.Contains(mod) && 
                !processedNewMods.Contains(mod.FileName)).ToList();
            
            foreach (var newMod in newMods)
            {
                if (!newMod.IsEnabled)
                {
                    continue;
                }
                
                finalOrder.Add(FormatLine(newMod, caseLookup));
            }
            
            await File.WriteAllLinesAsync(targetPath, finalOrder, Utf8NoBom).ConfigureAwait(false);
        }

        public static async Task<bool> ReEnableModAsync(AppConfigModel config, string modFileName)
        {
            if (string.IsNullOrWhiteSpace(modFileName))
            {
                throw new ArgumentException("Mod file name is required.", nameof(modFileName));
            }

            var (referenceMods, currentMods, targetPath) = await LoadModStateAsync(config);
            var comparer = StringComparer.OrdinalIgnoreCase;

            if (currentMods.Any(m => comparer.Equals(m.FileName, modFileName)))
            {
                return false;
            }

            var referenceMod = referenceMods.FirstOrDefault(m => comparer.Equals(m.FileName, modFileName));
            if (referenceMod is null)
            {
                throw new InvalidOperationException($"Reference file does not contain the mod '{modFileName}'.");
            }

            currentMods.Add(new ModEntryModel(referenceMod.ToLine()));
            await WriteAlignedLoadOrderAsync(config, referenceMods, currentMods, targetPath);
            return true;
        }

        public static async Task<bool> RemoveNewModAsync(AppConfigModel config, string modFileName)
        {
            if (string.IsNullOrWhiteSpace(modFileName))
            {
                throw new ArgumentException("Mod file name is required.", nameof(modFileName));
            }

            var (referenceMods, currentMods, targetPath) = await LoadModStateAsync(config);
            var comparer = StringComparer.OrdinalIgnoreCase;

            int removedCount = currentMods.RemoveAll(m => comparer.Equals(m.FileName, modFileName));
            if (removedCount == 0)
            {
                return false;
            }

            await WriteAlignedLoadOrderAsync(config, referenceMods, currentMods, targetPath);
            return true;
        }

        public static async Task<bool> ReplaceModWithNewAsync(AppConfigModel config, string removedModFileName, string replacementModFileName)
        {
            if (string.IsNullOrWhiteSpace(removedModFileName))
            {
                throw new ArgumentException("Removed mod name is required.", nameof(removedModFileName));
            }

            if (string.IsNullOrWhiteSpace(replacementModFileName))
            {
                throw new ArgumentException("Replacement mod name is required.", nameof(replacementModFileName));
            }

            var (referenceMods, currentMods, targetPath) = await LoadModStateAsync(config);
            var comparer = StringComparer.OrdinalIgnoreCase;

            int replacementIndex = currentMods.FindIndex(m => comparer.Equals(m.FileName, replacementModFileName));
            if (replacementIndex < 0)
            {
                return false;
            }

            currentMods.RemoveAt(replacementIndex);
            currentMods.RemoveAll(m => comparer.Equals(m.FileName, removedModFileName));

            int referenceIndex = referenceMods.FindIndex(m => comparer.Equals(m.FileName, removedModFileName));
            if (referenceIndex < 0)
            {
                throw new InvalidOperationException($"Reference file does not contain the mod '{removedModFileName}'.");
            }

            var referenceSlot = referenceMods[referenceIndex];
            var replacementEntry = new ModEntryModel($"*{replacementModFileName}", referenceSlot.LineNumber, referenceSlot.OriginalLineNumber);
            referenceMods[referenceIndex] = replacementEntry;
            currentMods.Add(new ModEntryModel(replacementEntry.ToLine()));

            await WriteAlignedLoadOrderAsync(config, referenceMods, currentMods, targetPath);
            return true;
        }

        /// <summary>
        /// Calculates the changes between current Plugins.txt and the reference file.
        /// Returns lists of added and removed mod names for version metadata.
        /// </summary>
        public static async Task<(List<string> AddedMods, List<string> RemovedMods)> CalculateReferenceChangesAsync(AppConfigModel config)
        {
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

            var referenceMods = await ReadFileAsync(referencePath, true);
            var currentMods = await ReadFileAsync(targetPath);

            var referenceModNames = new HashSet<string>(
                referenceMods.Select(m => m.FileName),
                StringComparer.OrdinalIgnoreCase);

            var currentModNames = new HashSet<string>(
                currentMods.Select(m => m.FileName),
                StringComparer.OrdinalIgnoreCase);

            var addedMods = currentModNames.Except(referenceModNames, StringComparer.OrdinalIgnoreCase).ToList();
            var removedMods = referenceModNames.Except(currentModNames, StringComparer.OrdinalIgnoreCase).ToList();

            return (addedMods, removedMods);
        }
    }
}
