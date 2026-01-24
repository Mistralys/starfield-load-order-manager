using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LoadOrderKeeper.ViewTexts
{
    /// <summary>
    /// Text ViewModel for ConfigInvalidOverlay.
    /// </summary>
    public partial class ConfigInvalidOverlayTexts : ObservableObject
    {
        private readonly LocalizationService _localization = LocalizationService.Instance;

        public ConfigInvalidOverlayTexts()
        {
            // Subscribe to culture changes
            _localization.CultureChanged += OnCultureChanged;
        }

        // Overlay texts
        public string Title => _localization.GetString("ConfigInvalidOverlay", "Title");
        public string MessageLine1 => _localization.GetString("ConfigInvalidOverlay", "MessageLine1");
        public string MessageLine2 => _localization.GetString("ConfigInvalidOverlay", "MessageLine2");

        /// <summary>
        /// Handles culture changes by refreshing all properties.
        /// </summary>
        private void OnCultureChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(MessageLine1));
            OnPropertyChanged(nameof(MessageLine2));
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
