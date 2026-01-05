using System;
using System.Collections.Generic;

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
        /// List of mod file names that were removed in this version.
        /// </summary>
        public List<string> RemovedMods { get; set; } = new();

        /// <summary>
        /// List of mod file names that were added in this version.
        /// </summary>
        public List<string> AddedMods { get; set; } = new();

        /// <summary>
        /// Total number of mods changed (added + removed).
        /// </summary>
        public int TotalModsChanged => RemovedMods.Count + AddedMods.Count;

        /// <summary>
        /// Gets a human-readable change summary for display.
        /// </summary>
        public string GetChangeSummary()
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(Comment))
            {
                parts.Add(Comment);
            }

            // Threshold for using numbers instead of listing names
            const int nameThreshold = 3;

            // Handle removed mods
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

            // Handle added mods
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

            if (parts.Count == 0)
            {
                return "No changes";
            }

            // Join with appropriate separators
            if (parts.Count == 1)
            {
                return parts[0];
            }

            // If comment exists, use " - " separator, otherwise use ", "
            if (!string.IsNullOrWhiteSpace(Comment))
            {
                var changesPart = string.Join(", ", parts.GetRange(1, parts.Count - 1));
                return $"{parts[0]} - {changesPart}";
            }

            return string.Join(", ", parts);
        }

        /// <summary>
        /// Formatted timestamp for display.
        /// </summary>
        public string FormattedTimestamp => Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
    }
}
