using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LoadOrderKeeper.ViewTexts
{
    /// <summary>
    /// Text ViewModel wrapping MainWindow localization strings.
    /// Provides INotifyPropertyChanged support for language switching.
    /// </summary>
    public partial class MainWindowTexts : ObservableObject
    {
        private readonly LocalizationService _localization = LocalizationService.Instance;

        public MainWindowTexts()
        {
            // Subscribe to culture changes
            _localization.CultureChanged += OnCultureChanged;
        }

        // Configuration Error Banner
        public string ConfigErrorBannerText => _localization.GetString("MainWindow", "ConfigErrorBannerText");
        public string OpenSettingsButtonText => _localization.GetString("MainWindow", "OpenSettingsButtonText");
        
        // Show Changes Button
        public string ShowChangesButtonText => _localization.GetString("MainWindow", "ShowChangesButtonText");
        public string ShowChangesButtonTextWithCount => _localization.GetString("MainWindow", "ShowChangesButtonTextWithCount");
        
        // Dismiss Tooltip
        public string DismissTooltip => _localization.GetString("MainWindow", "DismissTooltip");
        
        // Play Button
        public string PlayButtonSfse => _localization.GetString("MainWindow", "PlayButtonSfse");
        public string PlayButtonVanilla => _localization.GetString("MainWindow", "PlayButtonVanilla");
        
        // Update Message
        public string UpdateAvailableFormat => _localization.GetString("MainWindow", "UpdateAvailableFormat");

        /// <summary>
        /// Refreshes all localized properties when culture changes.
        /// </summary>
        private void OnCultureChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(ConfigErrorBannerText));
            OnPropertyChanged(nameof(OpenSettingsButtonText));
            OnPropertyChanged(nameof(ShowChangesButtonText));
            OnPropertyChanged(nameof(ShowChangesButtonTextWithCount));
            OnPropertyChanged(nameof(DismissTooltip));
            OnPropertyChanged(nameof(PlayButtonSfse));
            OnPropertyChanged(nameof(PlayButtonVanilla));
            OnPropertyChanged(nameof(UpdateAvailableFormat));
        }

        /// <summary>
        /// Legacy method for backward compatibility.
        /// </summary>
        public void RefreshAll()
        {
            OnCultureChanged(this, EventArgs.Empty);
        }
    }
}
