using System.Collections.Generic;
using System.Linq;

namespace LoadOrderKeeper.Models
{
    /// <summary>
    /// Represents pending changes that will be recorded in the next version history entry.
    /// These changes describe what was modified since the last reference update.
    /// </summary>
    public sealed class PendingChangesModel
    {
        /// <summary>
        /// Optional user comment describing the changes.
        /// </summary>
        public string? Comment { get; set; }

        /// <summary>
        /// List of mod file names that were added since the last reference update.
        /// </summary>
        public List<string> AddedMods { get; set; } = new();

        /// <summary>
        /// List of mod file names that were removed since the last reference update.
        /// </summary>
        public List<string> RemovedMods { get; set; } = new();

        /// <summary>
        /// Gets whether there are any pending changes.
        /// </summary>
        public bool IsEmpty => AddedMods.Count == 0 && RemovedMods.Count == 0;

        /// <summary>
        /// Total number of changes (added + removed).
        /// </summary>
        public int TotalChanges => AddedMods.Count + RemovedMods.Count;

        /// <summary>
        /// Creates an empty pending changes instance.
        /// </summary>
        public static PendingChangesModel CreateEmpty()
        {
            return new PendingChangesModel();
        }

        /// <summary>
        /// Creates a pending changes instance from lists of added and removed mods.
        /// </summary>
        public static PendingChangesModel Create(IReadOnlyList<string> addedMods, IReadOnlyList<string> removedMods)
        {
            return new PendingChangesModel
            {
                AddedMods = addedMods.ToList(),
                RemovedMods = removedMods.ToList()
            };
        }

        /// <summary>
        /// Creates a pending changes instance with comment, added mods, and removed mods.
        /// </summary>
        public static PendingChangesModel Create(string? comment, IReadOnlyList<string> addedMods, IReadOnlyList<string> removedMods)
        {
            return new PendingChangesModel
            {
                Comment = comment,
                AddedMods = addedMods.ToList(),
                RemovedMods = removedMods.ToList()
            };
        }
    }
}
