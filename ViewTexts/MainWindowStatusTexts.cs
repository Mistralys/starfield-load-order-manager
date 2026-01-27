using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LoadOrderKeeper.ViewTexts
{
    /// <summary>
    /// Text ViewModel wrapping MainWindow status message localization strings.
    /// Provides INotifyPropertyChanged support for language switching.
    /// </summary>
    public partial class MainWindowStatusTexts : ObservableObject
    {
        private readonly LocalizationService _localization = LocalizationService.Instance;

        public MainWindowStatusTexts()
        {
            // Subscribe to culture changes
            _localization.CultureChanged += OnCultureChanged;
        }

        // Status Messages
        public string ApplyingFix => _localization.GetString("MainWindowStatus", "ApplyingFix");
        public string FixAppliedSuccess => _localization.GetString("MainWindowStatus", "FixAppliedSuccess");
        public string ConfigUpdated => _localization.GetString("MainWindowStatus", "ConfigUpdated");
        public string ConfigInvalid => _localization.GetString("MainWindowStatus", "ConfigInvalid");
        public string DebugStateCopied => _localization.GetString("MainWindowStatus", "DebugStateCopied");
        public string ConfigReset => _localization.GetString("MainWindowStatus", "ConfigReset");
        public string ProfileSwitchedFormat => _localization.GetString("MainWindowStatus", "ProfileSwitchedFormat");

        /// <summary>
        /// Refreshes all localized properties when culture changes.
        /// </summary>
        private void OnCultureChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(ApplyingFix));
            OnPropertyChanged(nameof(FixAppliedSuccess));
            OnPropertyChanged(nameof(ConfigUpdated));
            OnPropertyChanged(nameof(ConfigInvalid));
            OnPropertyChanged(nameof(DebugStateCopied));
            OnPropertyChanged(nameof(ConfigReset));
            OnPropertyChanged(nameof(ProfileSwitchedFormat));
        }
    }
}
