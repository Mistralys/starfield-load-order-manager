using CommunityToolkit.Mvvm.ComponentModel;

namespace LoadOrderKeeper.ViewTexts
{
    /// <summary>
    /// Text ViewModel for DiffDialog with hardcoded strings.
    /// Will be connected to resource files in Phase 2.
    /// </summary>
    public partial class DiffDialogTexts : ObservableObject
    {
        // Window
        public string WindowTitle { get; } = "Load Order Changes";
        
        // Description
        public string DescriptionText { get; } = "The following changes were detected between your current load order and the reference:";
        
        // Buttons
        public string AcceptChangesButtonText { get; } = "Accept Changes";
        public string DiscardChangesButtonText { get; } = "Discard Changes";
        public string CloseButtonText { get; } = "Close";
        
        // Column Headers
        public string ChangeColumnHeader { get; } = "Change";
        public string ModNameColumnHeader { get; } = "Mod Name";
        public string PositionColumnHeader { get; } = "Position";
        
        // Change Types
        public string AddedText { get; } = "Added";
        public string RemovedText { get; } = "Removed";
        public string MovedText { get; } = "Moved";
        public string ReplacedText { get; } = "Replaced";
        
        /// <summary>
        /// Refreshes all localized properties when culture changes.
        /// </summary>
        public void RefreshAll()
        {
            OnPropertyChanged(nameof(WindowTitle));
            OnPropertyChanged(nameof(DescriptionText));
            OnPropertyChanged(nameof(AcceptChangesButtonText));
            OnPropertyChanged(nameof(DiscardChangesButtonText));
            OnPropertyChanged(nameof(CloseButtonText));
            OnPropertyChanged(nameof(ChangeColumnHeader));
            OnPropertyChanged(nameof(ModNameColumnHeader));
            OnPropertyChanged(nameof(PositionColumnHeader));
            OnPropertyChanged(nameof(AddedText));
            OnPropertyChanged(nameof(RemovedText));
            OnPropertyChanged(nameof(MovedText));
            OnPropertyChanged(nameof(ReplacedText));
        }
    }
}
