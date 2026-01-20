using CommunityToolkit.Mvvm.ComponentModel;

namespace LoadOrderKeeper.ViewTexts
{
    /// <summary>
    /// Text ViewModel wrapping MainWindowResources for main window UI text.
    /// Provides INotifyPropertyChanged support for language switching.
    /// </summary>
    public partial class MainWindowTexts : ObservableObject
    {
        // Configuration Error Banner
        public string ConfigErrorBannerText => Resources.MainWindowResources.ConfigErrorBannerText;
        public string OpenSettingsButtonText => Resources.MainWindowResources.OpenSettingsButtonText;
        
        // Show Changes Button
        public string ShowChangesButtonText => Resources.MainWindowResources.ShowChangesButtonText;
        public string ShowChangesButtonTextWithCount => Resources.MainWindowResources.ShowChangesButtonTextWithCount;
        
        // Dismiss Tooltip
        public string DismissTooltip => Resources.MainWindowResources.DismissTooltip;
        
        /// <summary>
        /// Refreshes all localized properties when culture changes.
        /// </summary>
        public void RefreshAll()
        {
            OnPropertyChanged(nameof(ConfigErrorBannerText));
            OnPropertyChanged(nameof(OpenSettingsButtonText));
            OnPropertyChanged(nameof(ShowChangesButtonText));
            OnPropertyChanged(nameof(ShowChangesButtonTextWithCount));
            OnPropertyChanged(nameof(DismissTooltip));
        }
    }
}
