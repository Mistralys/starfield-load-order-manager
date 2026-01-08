using System;

namespace LoadOrderKeeper.Coordinators.Events
{
    /// <summary>
    /// Event args for change detection events from FileMonitoringCoordinator.
    /// </summary>
    public sealed class ChangeDetectedEventArgs : EventArgs
    {
        public ChangeDetectedEventArgs(bool hasChanges, int changeCount)
        {
            HasChanges = hasChanges;
            ChangeCount = changeCount;
        }

        /// <summary>
        /// Whether changes were detected between current and reference files.
        /// </summary>
        public bool HasChanges { get; }

        /// <summary>
        /// Total number of changes detected (including dependent changes).
        /// </summary>
        public int ChangeCount { get; }
    }

    /// <summary>
    /// Event args for Steam warning state changes from FileMonitoringCoordinator.
    /// </summary>
    public sealed class SteamWarningChangedEventArgs : EventArgs
    {
        public SteamWarningChangedEventArgs(bool showWarning, string tooltip)
        {
            ShowWarning = showWarning;
            Tooltip = tooltip;
        }

        /// <summary>
        /// Whether the Steam warning should be displayed.
        /// </summary>
        public bool ShowWarning { get; }

        /// <summary>
        /// The tooltip text to display for the warning.
        /// </summary>
        public string Tooltip { get; }
    }

    /// <summary>
    /// Event args for sorting recommendation changes from FileMonitoringCoordinator.
    /// </summary>
    public sealed class SortingRecommendationChangedEventArgs : EventArgs
    {
        public SortingRecommendationChangedEventArgs(bool recommendSorting, string message)
        {
            RecommendSorting = recommendSorting;
            Message = message;
        }

        /// <summary>
        /// Whether sorting is recommended.
        /// </summary>
        public bool RecommendSorting { get; }

        /// <summary>
        /// The recommendation message to display.
        /// </summary>
        public string Message { get; }
    }
}
