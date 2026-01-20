using CommunityToolkit.Mvvm.ComponentModel;

namespace LoadOrderKeeper.ViewTexts
{
    /// <summary>
    /// Text ViewModel for ConfirmationDialog with hardcoded strings.
    /// Will be connected to resource files in Phase 2.
    /// </summary>
    public partial class ConfirmationDialogTexts : ObservableObject
    {
        // Buttons
        public string OkButtonText { get; } = "OK";
        public string CancelButtonText { get; } = "Cancel";
        public string YesButtonText { get; } = "Yes";
        public string NoButtonText { get; } = "No";
        
        /// <summary>
        /// Refreshes all localized properties when culture changes.
        /// </summary>
        public void RefreshAll()
        {
            OnPropertyChanged(nameof(OkButtonText));
            OnPropertyChanged(nameof(CancelButtonText));
            OnPropertyChanged(nameof(YesButtonText));
            OnPropertyChanged(nameof(NoButtonText));
        }
    }
}
