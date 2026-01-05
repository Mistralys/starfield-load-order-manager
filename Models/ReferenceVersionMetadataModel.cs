using System;
using System.Collections.Generic;
using LoadOrderKeeper.Services;

namespace LoadOrderKeeper.Models
{
    /// <summary>
    /// Metadata for a versioned reference file archive.
    /// </summary>
    public sealed class ReferenceVersionMetadataModel
    {
        /// <summary>
        /// Version number (starts at 1, increments automatically).
        /// </summary>
        public int VersionNumber { get; set; }

        /// <summary>
        /// Timestamp when this version was created.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Optional user comment describing the changes.
        /// </summary>
        public string? Comment { get; set; }

        /// <summary>
        /// List of mod file names that were removed when creating this version.
        /// These mods were in the previous reference but are not in this version.
        /// Rolling back to this version will remove these mods from Plugins.txt.
        /// </summary>
        public List<string> RemovedMods { get; set; } = new();

        /// <summary>
        /// List of mod file names that were added when creating this version.
        /// These mods were not in the previous reference but are in this version.
        /// Rolling back to this version will add these mods back to Plugins.txt.
        /// </summary>
        public List<string> AddedMods { get; set; } = new();

        /// <summary>
        /// Total number of mods changed (added + removed).
        /// </summary>
        public int TotalModsChanged => RemovedMods.Count + AddedMods.Count;

        /// <summary>
        /// Gets a human-readable change summary describing what changed when creating this version.
        /// </summary>
        public string GetChangeSummary()
        {
            var parts = new List<string>();

            // Threshold for using numbers instead of listing names
            const int nameThreshold = 3;

            // Describe what was added in this version
            if (AddedMods.Count > 0)
            {
                if (AddedMods.Count <= nameThreshold)
                {
                    parts.Add($"Added {string.Join(" and ", AddedMods)}");
                }
                else
                {
                    parts.Add($"Added {AddedMods.Count} mods");
                }
            }

            // Describe what was removed in this version
            if (RemovedMods.Count > 0)
            {
                if (RemovedMods.Count <= nameThreshold)
                {
                    parts.Add($"Removed {string.Join(" and ", RemovedMods)}");
                }
                else
                {
                    parts.Add($"Removed {RemovedMods.Count} mods");
                }
            }

            if (parts.Count == 0)
            {
                return "No changes";
            }

            return string.Join(", ", parts);
        }

        /// <summary>
        /// User-friendly formatted timestamp for display.
        /// </summary>
        public string FormattedTimestamp => DateTimeFormattingService.FormatFriendly(Timestamp);
    }
}
