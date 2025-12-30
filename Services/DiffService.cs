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

            foreach (var diff in diffs)
            {
                string displayName = $"*{diff.FileName}";
                if (diff.IsRemoved)
                {
                    string text = $"#{diff.ReferenceNumber}: {displayName} removed from load order";
                    result.Add(new DiffLineModel(text, DiffChangeType.Removed));
                }
                else if (diff.IsNew)
                {
                    string text = $"#{diff.CurrentNumber}: {displayName} added to load order";
                    result.Add(new DiffLineModel(text, DiffChangeType.Added));
                }
                else if (diff.IsMoved)
                {
                    string text = $"{displayName} moved from #{diff.ReferenceNumber} to #{diff.CurrentNumber}";
                    result.Add(new DiffLineModel(text, DiffChangeType.Moved));
                }
            }

            return result;
        }
    }
}
