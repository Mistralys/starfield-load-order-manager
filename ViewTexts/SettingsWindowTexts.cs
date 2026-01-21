using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LoadOrderKeeper.ViewTexts
{
    /// <summary>
    /// Text ViewModel for SettingsWindow.
    /// </summary>
    public partial class SettingsWindowTexts : ObservableObject
    {
        private readonly LocalizationService _localization = LocalizationService.Instance;

        public SettingsWindowTexts()
        {
            // Subscribe to culture changes
            _localization.CultureChanged += OnCultureChanged;
        }

        // Window
        public string WindowTitle => _localization.GetString("Settings", "WindowTitle");
        
        // Labels
        public string AppDataPathLabel => _localization.GetString("Settings", "AppDataPathLabel");
        public string GamePathLabel => _localization.GetString("Settings", "GamePathLabel");
        public string DetectedPathsLabel => _localization.GetString("Settings", "DetectedPathsLabel");
        
        // Buttons
        public string BrowseButtonText => _localization.GetString("Settings", "BrowseButtonText");
        public string UseDetectedAppDataButtonText => _localization.GetString("Settings", "UseDetectedAppDataButtonText");
        public string UseDetectedGamePathButtonText => _localization.GetString("Settings", "UseDetectedGamePathButtonText");
        public string SaveButtonText => _localization.GetString("Settings", "SaveButtonText");
        public string CancelButtonText => _localization.GetString("Settings", "CancelButtonText");
        
        // Status Messages
        public string ValidConfigMessage => _localization.GetString("Settings", "ValidConfigMessage");
        public string InvalidAppDataMessage => _localization.GetString("Settings", "InvalidAppDataMessage");
        public string InvalidGamePathMessage => _localization.GetString("Settings", "InvalidGamePathMessage");
        public string AppDataNotConfiguredMessage => _localization.GetString("Settings", "AppDataNotConfiguredMessage");
        public string GamePathNotConfiguredMessage => _localization.GetString("Settings", "GamePathNotConfiguredMessage");
        public string GameDataFolderNotFoundMessage => _localization.GetString("Settings", "GameDataFolderNotFoundMessage");
        public string PluginsTxtNotFoundMessage => _localization.GetString("Settings", "PluginsTxtNotFoundMessage");
        public string ProfilesFolderAccessDeniedMessage => _localization.GetString("Settings", "ProfilesFolderAccessDeniedMessage");
        public string ProfilesFolderCannotBeCreatedMessage => _localization.GetString("Settings", "ProfilesFolderCannotBeCreatedMessage");
        public string BothPathsInvalidMessage => _localization.GetString("Settings", "BothPathsInvalidMessage");

        /// <summary>
        /// Handles culture changes by refreshing all properties.
        /// </summary>
        private void OnCultureChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(WindowTitle));
            OnPropertyChanged(nameof(AppDataPathLabel));
            OnPropertyChanged(nameof(GamePathLabel));
            OnPropertyChanged(nameof(DetectedPathsLabel));
            OnPropertyChanged(nameof(BrowseButtonText));
            OnPropertyChanged(nameof(UseDetectedAppDataButtonText));
            OnPropertyChanged(nameof(UseDetectedGamePathButtonText));
            OnPropertyChanged(nameof(SaveButtonText));
            OnPropertyChanged(nameof(CancelButtonText));
            OnPropertyChanged(nameof(ValidConfigMessage));
            OnPropertyChanged(nameof(InvalidAppDataMessage));
            OnPropertyChanged(nameof(InvalidGamePathMessage));
            OnPropertyChanged(nameof(AppDataNotConfiguredMessage));
            OnPropertyChanged(nameof(GamePathNotConfiguredMessage));
            OnPropertyChanged(nameof(GameDataFolderNotFoundMessage));
            OnPropertyChanged(nameof(PluginsTxtNotFoundMessage));
            OnPropertyChanged(nameof(ProfilesFolderAccessDeniedMessage));
            OnPropertyChanged(nameof(ProfilesFolderCannotBeCreatedMessage));
            OnPropertyChanged(nameof(BothPathsInvalidMessage));
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
