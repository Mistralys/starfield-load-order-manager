using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LoadOrderKeeper.ViewTexts
{
    /// <summary>
    /// Text ViewModel for SwitchProfileWindow.
    /// </summary>
    public partial class SwitchProfileTexts : ObservableObject
    {
        private readonly LocalizationService _localization = LocalizationService.Instance;

        public SwitchProfileTexts()
        {
            // Subscribe to culture changes
            _localization.CultureChanged += OnCultureChanged;
        }

        // Window
        public string WindowTitle => _localization.GetString("SwitchProfile", "WindowTitle");
        
        // Description
        public string DescriptionText => _localization.GetString("SwitchProfile", "DescriptionText");
        
        // Buttons
        public string SwitchButtonText => _localization.GetString("SwitchProfile", "SwitchButtonText");
        public string CancelButtonText => _localization.GetString("SwitchProfile", "CancelButtonText");
        
        // Messages
        public string CurrentProfileLabel => _localization.GetString("SwitchProfile", "CurrentProfileLabel");

        /// <summary>
        /// Handles culture changes by refreshing all properties.
        /// </summary>
        private void OnCultureChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(WindowTitle));
            OnPropertyChanged(nameof(DescriptionText));
            OnPropertyChanged(nameof(SwitchButtonText));
            OnPropertyChanged(nameof(CancelButtonText));
            OnPropertyChanged(nameof(CurrentProfileLabel));
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
