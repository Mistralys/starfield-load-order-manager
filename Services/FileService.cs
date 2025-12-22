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

            var files = Directory.EnumerateFiles(dataPath, "*.esm", SearchOption.TopDirectoryOnly)
                .Concat(Directory.EnumerateFiles(dataPath, "*.esp", SearchOption.TopDirectoryOnly));

            return files.ToDictionary(
                p => Path.GetFileName(p).ToLowerInvariant(),
                p => Path.GetFileName(p));
        }

        private static async Task<List<ModEntryModel>> ReadFileAsync(string filePath)
        {
            if (!File.Exists(filePath)) return new List<ModEntryModel>();

            var lines = await File.ReadAllLinesAsync(filePath, Encoding.UTF8);

            return lines
                .Where(line => !string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith("#"))
                .Select(line => new ModEntryModel(line))
                .ToList();
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

            string content = await File.ReadAllTextAsync(targetPath, Encoding.UTF8);
            await File.WriteAllTextAsync(referencePath, content, Utf8NoBom);
        }

        public static async Task ApplyLoadOrderAsync(AppConfigModel config)
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

            var caseLookup = GetCaseLookup(config.StarfieldGamePath);
            var referenceMods = await ReadFileAsync(referencePath);
            var currentMods = await ReadFileAsync(targetPath);

            var currentModSet = new HashSet<ModEntryModel>(currentMods);
            var newMods = currentMods.Where(mod => !referenceMods.Contains(mod)).ToList();

            var finalOrder = new List<string>();

            foreach (var referenceMod in referenceMods)
            {
                if (currentModSet.Contains(referenceMod))
                {
                    finalOrder.Add(FormatLine(referenceMod, caseLookup));
                }
            }

            foreach (var newMod in newMods)
            {
                finalOrder.Add(FormatLine(newMod, caseLookup));
            }

            await File.WriteAllLinesAsync(targetPath, finalOrder, Utf8NoBom);
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
            var cleanFileName = mod.FileName.ToLowerInvariant();
            var resolvedName = caseLookup.TryGetValue(cleanFileName, out var correctCase)
                ? correctCase
                : mod.FileName;

            return $"*{resolvedName}";
        }
    }
}
