using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LoadOrderKeeper.ViewTexts
{
    /// <summary>
    /// Text ViewModel for UpdateOptionsWindow.
    /// </summary>
    public partial class UpdateOptionsTexts : ObservableObject
    {
        private readonly LocalizationService _localization = LocalizationService.Instance;

        public UpdateOptionsTexts()
        {
            // Subscribe to culture changes
            _localization.CultureChanged += OnCultureChanged;
        }

        // Window
        public string WindowTitleDownload => _localization.GetString("UpdateOptions", "WindowTitleDownload");
        public string WindowTitleOptions => _localization.GetString("UpdateOptions", "WindowTitleOptions");
        
        // Buttons
        public string NexusmodsButtonText => _localization.GetString("UpdateOptions", "NexusmodsButtonText");
        public string GitHubButtonText => _localization.GetString("UpdateOptions", "GitHubButtonText");
        public string CancelButtonText => _localization.GetString("UpdateOptions", "CancelButtonText");
        
        // Messages
        public string MessageUpdateAvailable => _localization.GetString("UpdateOptions", "MessageUpdateAvailable");
        public string MessageCheckFailed => _localization.GetString("UpdateOptions", "MessageCheckFailed");

        /// <summary>
        /// Handles culture changes by refreshing all properties.
        /// </summary>
        private void OnCultureChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(WindowTitleDownload));
            OnPropertyChanged(nameof(WindowTitleOptions));
            OnPropertyChanged(nameof(NexusmodsButtonText));
            OnPropertyChanged(nameof(GitHubButtonText));
            OnPropertyChanged(nameof(CancelButtonText));
            OnPropertyChanged(nameof(MessageUpdateAvailable));
            OnPropertyChanged(nameof(MessageCheckFailed));
        }

        /// <summary>
        /// Refreshes all localized properties when culture changes (legacy compatibility).
        /// </summary>
        public void RefreshAll()
        {
            OnCultureChanged(this, EventArgs.Empty);
        }
    }
}
