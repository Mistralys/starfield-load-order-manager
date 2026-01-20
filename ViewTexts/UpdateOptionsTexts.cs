using CommunityToolkit.Mvvm.ComponentModel;

namespace LoadOrderKeeper.ViewTexts
{
    /// <summary>
    /// Text ViewModel for UpdateOptionsWindow with hardcoded strings.
    /// Will be connected to resource files in Phase 2.
    /// </summary>
    public partial class UpdateOptionsTexts : ObservableObject
    {
        // Window
        public string WindowTitleDownload { get; } = "Download Update";
        public string WindowTitleOptions { get; } = "Download Options";
        
        // Buttons
        public string NexusmodsButtonText { get; } = "Open on Nexusmods";
        public string GitHubButtonText { get; } = "Open on GitHub";
        public string CancelButtonText { get; } = "Cancel";
        
        /// <summary>
        /// Refreshes all localized properties when culture changes.
        /// </summary>
        public void RefreshAll()
        {
            OnPropertyChanged(nameof(WindowTitleDownload));
            OnPropertyChanged(nameof(WindowTitleOptions));
            OnPropertyChanged(nameof(NexusmodsButtonText));
            OnPropertyChanged(nameof(GitHubButtonText));
            OnPropertyChanged(nameof(CancelButtonText));
        }
    }
}
