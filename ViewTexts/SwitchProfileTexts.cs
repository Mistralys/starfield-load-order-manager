using CommunityToolkit.Mvvm.ComponentModel;

namespace LoadOrderKeeper.ViewTexts
{
    /// <summary>
    /// Text ViewModel for SwitchProfileWindow with hardcoded strings.
    /// Will be connected to resource files in Phase 2.
    /// </summary>
    public partial class SwitchProfileTexts : ObservableObject
    {
        // Window
        public string WindowTitle { get; } = "Switch Profile";
        
        // Description
        public string DescriptionText { get; } = "Select a profile to switch to:";
        
        // Buttons
        public string SwitchButtonText { get; } = "Switch";
        public string CancelButtonText { get; } = "Cancel";
        
        // Messages
        public string CurrentProfileLabel { get; } = "Current Profile";
        
        /// <summary>
        /// Refreshes all localized properties when culture changes.
        /// </summary>
        public void RefreshAll()
        {
            OnPropertyChanged(nameof(WindowTitle));
            OnPropertyChanged(nameof(DescriptionText));
            OnPropertyChanged(nameof(SwitchButtonText));
            OnPropertyChanged(nameof(CancelButtonText));
            OnPropertyChanged(nameof(CurrentProfileLabel));
        }
    }
}
