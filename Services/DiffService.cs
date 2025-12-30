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

            var currentLines = await File.ReadAllLinesAsync(targetPath).ConfigureAwait(false);
            var referenceLines = await File.ReadAllLinesAsync(referencePath).ConfigureAwait(false);

            return BuildDiff(referenceLines, currentLines);
        }

        private static IReadOnlyList<DiffLineModel> BuildDiff(string[] referenceLines, string[] currentLines)
        {
            var result = new List<DiffLineModel>();
            var lcs = BuildLcsMatrix(referenceLines, currentLines);

            int i = 0;
            int j = 0;
            while (i < referenceLines.Length && j < currentLines.Length)
            {
                if (string.Equals(referenceLines[i], currentLines[j], StringComparison.Ordinal))
                {
                    result.Add(new DiffLineModel(referenceLines[i], DiffChangeType.Unchanged));
                    i++;
                    j++;
                }
                else if (lcs[i + 1, j] >= lcs[i, j + 1])
                {
                    result.Add(new DiffLineModel(referenceLines[i], DiffChangeType.Removed));
                    i++;
                }
                else
                {
                    result.Add(new DiffLineModel(currentLines[j], DiffChangeType.Added));
                    j++;
                }
            }

            while (i < referenceLines.Length)
            {
                result.Add(new DiffLineModel(referenceLines[i++], DiffChangeType.Removed));
            }

            while (j < currentLines.Length)
            {
                result.Add(new DiffLineModel(currentLines[j++], DiffChangeType.Added));
            }

            return result;
        }

        private static int[,] BuildLcsMatrix(string[] referenceLines, string[] currentLines)
        {
            int m = referenceLines.Length;
            int n = currentLines.Length;
            var lcs = new int[m + 1, n + 1];

            for (int i = m - 1; i >= 0; i--)
            {
                for (int j = n - 1; j >= 0; j--)
                {
                    if (string.Equals(referenceLines[i], currentLines[j], StringComparison.Ordinal))
                    {
                        lcs[i, j] = lcs[i + 1, j + 1] + 1;
                    }
                    else
                    {
                        lcs[i, j] = Math.Max(lcs[i + 1, j], lcs[i, j + 1]);
                    }
                }
            }

            return lcs;
        }
    }
}
