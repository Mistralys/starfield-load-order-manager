using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LoadOrderKeeper.ViewTexts
{
    /// <summary>
    /// Text ViewModel wrapping ViewModelInitializer status message localization strings.
    /// Provides INotifyPropertyChanged support for language switching.
    /// </summary>
    public partial class ViewModelInitializerStatusTexts : ObservableObject
    {
        private readonly LocalizationService _localization = LocalizationService.Instance;

        public ViewModelInitializerStatusTexts()
        {
            // Subscribe to culture changes
            _localization.CultureChanged += OnCultureChanged;
        }

        // Status Messages
        public string PluginsTxtNotFoundFormat => _localization.GetString("ViewModelInitializerStatus", "PluginsTxtNotFoundFormat");
        public string NoReferenceCreating => _localization.GetString("ViewModelInitializerStatus", "NoReferenceCreating");
        public string ReferenceCreatedAuto => _localization.GetString("ViewModelInitializerStatus", "ReferenceCreatedAuto");
        public string FailedToCreateReferenceFormat => _localization.GetString("ViewModelInitializerStatus", "FailedToCreateReferenceFormat");

        /// <summary>
        /// Refreshes all localized properties when culture changes.
        /// </summary>
        private void OnCultureChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(PluginsTxtNotFoundFormat));
            OnPropertyChanged(nameof(NoReferenceCreating));
            OnPropertyChanged(nameof(ReferenceCreatedAuto));
            OnPropertyChanged(nameof(FailedToCreateReferenceFormat));
        }
    }
}
