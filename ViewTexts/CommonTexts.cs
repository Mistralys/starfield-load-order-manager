using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LoadOrderKeeper.ViewTexts
{
    /// <summary>
    /// Text ViewModel wrapping Common localization strings for common UI text across the application.
    /// Provides INotifyPropertyChanged support for language switching.
    /// </summary>
    public partial class CommonTexts : ObservableObject
    {
        private readonly LocalizationService _localization = LocalizationService.Instance;

        public CommonTexts()
        {
            // Subscribe to culture changes
            _localization.CultureChanged += OnCultureChanged;
        }

        // Common Buttons
        public string ButtonCancel => _localization.GetString("Common", "ButtonCancel");
        public string ButtonClose => _localization.GetString("Common", "ButtonClose");
        public string ButtonNo => _localization.GetString("Common", "ButtonNo");
        public string ButtonOk => _localization.GetString("Common", "ButtonOk");
        public string ButtonSave => _localization.GetString("Common", "ButtonSave");
        public string ButtonYes => _localization.GetString("Common", "ButtonYes");
        
        // Configuration Messages
        public string ConfigInvalidGuidance => _localization.GetString("Common", "ConfigInvalidGuidance");
        public string ErrorPrefix => _localization.GetString("Common", "ErrorPrefix");
        public string PluginsTxtRequired => _localization.GetString("Common", "PluginsTxtRequired");
        public string ProfilesFolderAccessDenied => _localization.GetString("Common", "ProfilesFolderAccessDenied");
        public string ProfilesFolderRequired => _localization.GetString("Common", "ProfilesFolderRequired");

        /// <summary>
        /// Handles culture changes by refreshing all properties.
        /// </summary>
        private void OnCultureChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(ButtonCancel));
            OnPropertyChanged(nameof(ButtonClose));
            OnPropertyChanged(nameof(ButtonNo));
            OnPropertyChanged(nameof(ButtonOk));
            OnPropertyChanged(nameof(ButtonSave));
            OnPropertyChanged(nameof(ButtonYes));
            OnPropertyChanged(nameof(ConfigInvalidGuidance));
            OnPropertyChanged(nameof(ErrorPrefix));
            OnPropertyChanged(nameof(PluginsTxtRequired));
            OnPropertyChanged(nameof(ProfilesFolderAccessDenied));
            OnPropertyChanged(nameof(ProfilesFolderRequired));
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
