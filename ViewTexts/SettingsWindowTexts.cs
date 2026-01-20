using CommunityToolkit.Mvvm.ComponentModel;

namespace LoadOrderKeeper.ViewTexts
{
    /// <summary>
    /// Text ViewModel for SettingsWindow with hardcoded strings.
    /// Will be connected to resource files in Phase 2.
    /// </summary>
    public partial class SettingsWindowTexts : ObservableObject
    {
        // Window
        public string WindowTitle { get; } = "Settings";
        
        // Labels
        public string AppDataPathLabel { get; } = "Starfield App Data Path:";
        public string GamePathLabel { get; } = "Starfield Game Path:";
        public string DetectedPathsLabel { get; } = "Detected Paths";
        
        // Buttons
        public string BrowseButtonText { get; } = "Browse...";
        public string UseDetectedAppDataButtonText { get; } = "Use Detected Path";
        public string UseDetectedGamePathButtonText { get; } = "Use Detected Path";
        public string SaveButtonText { get; } = "Save";
        public string CancelButtonText { get; } = "Cancel";
        
        // Status Messages
        public string ValidConfigMessage { get; } = "The configured paths are valid.";
        public string InvalidAppDataMessage { get; } = "The app data path is invalid";
        public string InvalidGamePathMessage { get; } = "The game path is invalid";
        public string AppDataNotConfiguredMessage { get; } = "The app data path is not configured";
        public string GamePathNotConfiguredMessage { get; } = "The game path is not configured";
        public string GameDataFolderNotFoundMessage { get; } = "The game Data folder was not found";
        public string PluginsTxtNotFoundMessage { get; } = "Plugins.txt not found in the app data folder";
        public string ProfilesFolderCannotBeCreatedMessage { get; } = "The Profiles folder cannot be created or accessed";
        public string BothPathsInvalidMessage { get; } = "Both the game path and app data path are invalid.";
        
        /// <summary>
        /// Refreshes all localized properties when culture changes.
        /// </summary>
        public void RefreshAll()
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
            OnPropertyChanged(nameof(ProfilesFolderCannotBeCreatedMessage));
            OnPropertyChanged(nameof(BothPathsInvalidMessage));
        }
    }
}
