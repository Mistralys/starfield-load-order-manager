using CommunityToolkit.Mvvm.ComponentModel;

namespace LoadOrderKeeper.ViewTexts
{
    /// <summary>
    /// Text ViewModel for ReferenceHistoryWindow with hardcoded strings.
    /// Will be connected to resource files in Phase 2.
    /// </summary>
    public partial class ReferenceHistoryTexts : ObservableObject
    {
        // Window
        public string WindowTitle { get; } = "Reference File Version History";
        
        // Menu
        public string FileMenuText { get; } = "_File";
        public string ExitMenuText { get; } = "E_xit";
        public string EditMenuText { get; } = "_Edit";
        public string ClearHistoryMenuText { get; } = "Clear the _history...";
        
        // Buttons
        public string RollbackButtonText { get; } = "Rollback to selected version...";
        public string DeleteVersionButtonText { get; } = "Delete version";
        public string ClearHistoryButtonText { get; } = "Clear the history...";
        public string CloseButtonText { get; } = "Close";
        
        // Column Headers
        public string VersionColumnHeader { get; } = "Version";
        public string DateColumnHeader { get; } = "Date & Time";
        public string ChangesColumnHeader { get; } = "Changes";
        public string SummaryColumnHeader { get; } = "Summary";
        
        // Messages
        public string NoVersionsMessage { get; } = "No version history available.";
        
        /// <summary>
        /// Refreshes all localized properties when culture changes.
        /// </summary>
        public void RefreshAll()
        {
            OnPropertyChanged(nameof(WindowTitle));
            OnPropertyChanged(nameof(FileMenuText));
            OnPropertyChanged(nameof(ExitMenuText));
            OnPropertyChanged(nameof(EditMenuText));
            OnPropertyChanged(nameof(ClearHistoryMenuText));
            OnPropertyChanged(nameof(RollbackButtonText));
            OnPropertyChanged(nameof(DeleteVersionButtonText));
            OnPropertyChanged(nameof(ClearHistoryButtonText));
            OnPropertyChanged(nameof(CloseButtonText));
            OnPropertyChanged(nameof(VersionColumnHeader));
            OnPropertyChanged(nameof(DateColumnHeader));
            OnPropertyChanged(nameof(ChangesColumnHeader));
            OnPropertyChanged(nameof(SummaryColumnHeader));
            OnPropertyChanged(nameof(NoVersionsMessage));
        }
    }
}
