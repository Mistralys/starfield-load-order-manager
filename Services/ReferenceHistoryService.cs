using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LoadOrderKeeper.Models;

namespace LoadOrderKeeper.Services
{
    /// <summary>
    /// Manages reference file version history including archiving, loading, and pruning.
    /// </summary>
    public static class ReferenceHistoryService
    {
        private const int MaxVersions = 16;
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

        /// <summary>
        /// Gets the history folder path for the active profile.
        /// </summary>
        public static string GetHistoryFolder(AppConfigModel config)
        {
            var profileId = config.ActiveProfileId ?? "default";
            return Path.Combine(ProfileService.GetProfileFolder(config, profileId), "History");
        }

        /// <summary>
        /// Archives the current reference file with metadata before updating it.
        /// </summary>
        /// <param name="config">Application configuration</param>
        /// <param name="comment">Optional user comment describing the changes</param>
        /// <param name="addedMods">List of mod names that were added</param>
        /// <param name="removedMods">List of mod names that were removed</param>
        /// <returns>The created version number</returns>
        public static async Task<int> ArchiveCurrentReferenceAsync(
            AppConfigModel config,
            string? comment,
            IReadOnlyList<string> addedMods,
            IReadOnlyList<string> removedMods)
        {
            if (!config.IsValid())
            {
                throw new InvalidOperationException("Configuration is not valid.");
            }

            var referencePath = config.GetReferenceFilePath();
            if (!File.Exists(referencePath))
            {
                throw new FileNotFoundException("Reference file not found.", referencePath);
            }

            // Ensure history folder exists
            var historyFolder = GetHistoryFolder(config);
            if (!Directory.Exists(historyFolder))
            {
                Directory.CreateDirectory(historyFolder);
            }

            // Determine next version number
            var existingVersions = await LoadVersionHistoryAsync(config);
            var nextVersion = existingVersions.Count == 0 ? 1 : existingVersions.Max(v => v.VersionNumber) + 1;

            // Create metadata
            var metadata = new ReferenceVersionMetadataModel
            {
                VersionNumber = nextVersion,
                Timestamp = DateTime.Now,
                Comment = comment,
                AddedMods = addedMods.ToList(),
                RemovedMods = removedMods.ToList()
            };

            // Archive files
            var versionFileName = $"reference_v{nextVersion}.txt";
            var metadataFileName = $"reference_v{nextVersion}.json";
            var versionFilePath = Path.Combine(historyFolder, versionFileName);
            var metadataFilePath = Path.Combine(historyFolder, metadataFileName);

            try
            {
                // Copy reference file
                var referenceContent = await File.ReadAllTextAsync(referencePath, Encoding.UTF8);
                await File.WriteAllTextAsync(versionFilePath, referenceContent, Utf8NoBom);

                // Write metadata
                var metadataJson = JsonSerializer.Serialize(metadata, JsonOptions);
                await File.WriteAllTextAsync(metadataFilePath, metadataJson, Utf8NoBom);
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to archive reference file: {ex.Message}", ex);
            }

            // Prune old versions if we exceed the limit
            await PruneOldVersionsAsync(config);

            return nextVersion;
        }

        /// <summary>
        /// Loads all version history metadata for the active profile.
        /// </summary>
        public static async Task<IReadOnlyList<ReferenceVersionMetadataModel>> LoadVersionHistoryAsync(AppConfigModel config)
        {
            if (!config.IsValid())
            {
                return Array.Empty<ReferenceVersionMetadataModel>();
            }

            var historyFolder = GetHistoryFolder(config);
            if (!Directory.Exists(historyFolder))
            {
                return Array.Empty<ReferenceVersionMetadataModel>();
            }

            var versions = new List<ReferenceVersionMetadataModel>();
            var metadataFiles = Directory.GetFiles(historyFolder, "reference_v*.json");

            foreach (var metadataFile in metadataFiles)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(metadataFile, Encoding.UTF8);
                    var metadata = JsonSerializer.Deserialize<ReferenceVersionMetadataModel>(json);

                    if (metadata != null)
                    {
                        // Verify corresponding reference file exists
                        var versionFileName = $"reference_v{metadata.VersionNumber}.txt";
                        var versionFilePath = Path.Combine(historyFolder, versionFileName);

                        if (File.Exists(versionFilePath))
                        {
                            versions.Add(metadata);
                        }
                    }
                }
                catch
                {
                    // Silently ignore corrupted or invalid metadata files
                }
            }

            // Sort by version number descending (newest first)
            return versions.OrderByDescending(v => v.VersionNumber).ToList();
        }

        /// <summary>
        /// Gets the file path for a specific version's reference file.
        /// </summary>
        public static string GetVersionFilePath(AppConfigModel config, int versionNumber)
        {
            var historyFolder = GetHistoryFolder(config);
            return Path.Combine(historyFolder, $"reference_v{versionNumber}.txt");
        }

        /// <summary>
        /// Deletes a specific version from history.
        /// </summary>
        public static async Task DeleteVersionAsync(AppConfigModel config, int versionNumber)
        {
            if (!config.IsValid())
            {
                throw new InvalidOperationException("Configuration is not valid.");
            }

            var historyFolder = GetHistoryFolder(config);
            var versionFilePath = Path.Combine(historyFolder, $"reference_v{versionNumber}.txt");
            var metadataFilePath = Path.Combine(historyFolder, $"reference_v{versionNumber}.json");

            try
            {
                if (File.Exists(versionFilePath))
                {
                    File.Delete(versionFilePath);
                }

                if (File.Exists(metadataFilePath))
                {
                    File.Delete(metadataFilePath);
                }
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to delete version {versionNumber}: {ex.Message}", ex);
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Clears all version history for the active profile.
        /// </summary>
        public static async Task ClearHistoryAsync(AppConfigModel config)
        {
            if (!config.IsValid())
            {
                throw new InvalidOperationException("Configuration is not valid.");
            }

            var historyFolder = GetHistoryFolder(config);
            if (!Directory.Exists(historyFolder))
            {
                return;
            }

            try
            {
                Directory.Delete(historyFolder, true);
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to clear history: {ex.Message}", ex);
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Prunes old versions to maintain the maximum version limit.
        /// </summary>
        private static async Task PruneOldVersionsAsync(AppConfigModel config)
        {
            var versions = await LoadVersionHistoryAsync(config);

            if (versions.Count <= MaxVersions)
            {
                return;
            }

            // Delete oldest versions (those with lowest version numbers)
            var versionsToDelete = versions
                .OrderBy(v => v.VersionNumber)
                .Take(versions.Count - MaxVersions)
                .ToList();

            foreach (var version in versionsToDelete)
            {
                try
                {
                    await DeleteVersionAsync(config, version.VersionNumber);
                }
                catch
                {
                    // Continue deleting other versions even if one fails
                }
            }
        }

        /// <summary>
        /// Rolls back to a specific version by replacing Plugins.txt with the archived version.
        /// This allows the user to review changes in the DIFF window before accepting.
        /// </summary>
        public static async Task RollbackToVersionAsync(AppConfigModel config, int versionNumber)
        {
            if (!config.IsValid())
            {
                throw new InvalidOperationException("Configuration is not valid.");
            }

            var versionFilePath = GetVersionFilePath(config, versionNumber);
            if (!File.Exists(versionFilePath))
            {
                throw new FileNotFoundException($"Version {versionNumber} not found.", versionFilePath);
            }

            var pluginsPath = config.GetPluginsFilePath();

            try
            {
                // Copy archived version to Plugins.txt
                var versionContent = await File.ReadAllTextAsync(versionFilePath, Encoding.UTF8);
                await File.WriteAllTextAsync(pluginsPath, versionContent, Utf8NoBom);
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to rollback to version {versionNumber}: {ex.Message}", ex);
            }
        }
    }
}
