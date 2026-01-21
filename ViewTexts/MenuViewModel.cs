using CommunityToolkit.Mvvm.ComponentModel;
using LoadOrderKeeper.Services;

namespace LoadOrderKeeper.ViewTexts
{
    /// <summary>
    /// ViewModel for menu and UI text properties.
    /// Centralizes all static text strings for menus, labels, and buttons.
    /// </summary>
    public partial class MenuViewModel : ObservableObject
    {
        private readonly LocalizationService _localization = LocalizationService.Instance;

        public MenuViewModel()
        {
            // Subscribe to culture changes to refresh all properties
            _localization.CultureChanged += OnCultureChanged;
        }

        // Window Title
        public string WindowTitle => 
            _localization.GetString("Menu", "WindowTitleFormat", VersionService.GetApplicationVersion());

        // File Menu
        public string FileMenuHeader => _localization.GetString("Menu", "FileMenuHeader");
        public string OpenPluginsMenuText => _localization.GetString("Menu", "OpenPluginsMenuText");
        public string OpenReferenceMenuText => _localization.GetString("Menu", "OpenReferenceMenuText");
        public string OpenAppDataFolderMenuText => _localization.GetString("Menu", "OpenAppDataFolderMenuText");
        public string OpenGameFolderMenuText => _localization.GetString("Menu", "OpenGameFolderMenuText");
        public string ExitMenuText => _localization.GetString("Menu", "ExitMenuText");

        // Edit Menu
        public string EditMenuHeader => _localization.GetString("Menu", "EditMenuHeader");
        public string SettingsMenuText => _localization.GetString("Menu", "SettingsMenuText");

        // Profile Menu
        public string ProfileMenuHeader => _localization.GetString("Menu", "ProfileMenuHeader");
        public string SwitchProfileMenuText => _localization.GetString("Menu", "SwitchProfileMenuText");
        public string ManageProfilesMenuText => _localization.GetString("Menu", "ManageProfilesMenuText");
        public string ReferenceHistoryMenuText => _localization.GetString("Menu", "ReferenceHistoryMenuText");
        public string ViewPendingChangesMenuText => _localization.GetString("Menu", "ViewPendingChangesMenuText");

        // Help Menu
        public string HelpMenuHeader => _localization.GetString("Menu", "HelpMenuHeader");
        public string CheckForUpdatesMenuText => _localization.GetString("Menu", "CheckForUpdatesMenuText");
        public string AboutMenuText => _localization.GetString("Menu", "AboutMenuText");

        // Debug Menu
        public string DebugMenuHeader => _localization.GetString("Menu", "DebugMenuHeader");
        public string ResetConfigMenuText => _localization.GetString("Menu", "ResetConfigMenuText");
        public string OpenConfigFolderMenuText => _localization.GetString("Menu", "OpenConfigFolderMenuText");
        public string ThrowTestExceptionMenuText => _localization.GetString("Menu", "ThrowTestExceptionMenuText");

        // Button Text
        public string DownloadOptionsButtonText => _localization.GetString("Menu", "DownloadOptionsButtonText");

        // Labels
        public string ActiveProfilePrefixText => _localization.GetString("Menu", "ActiveProfilePrefixText");
        public string RecentStatusMessagesText => _localization.GetString("Menu", "RecentStatusMessagesText");

        // Warning Messages
        public string PluginsModifiedWarningText => _localization.GetString("Menu", "PluginsModifiedWarningText");

        /// <summary>
        /// Refreshes all localized properties when culture changes.
        /// </summary>
        private void OnCultureChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(WindowTitle));
            OnPropertyChanged(nameof(FileMenuHeader));
            OnPropertyChanged(nameof(OpenPluginsMenuText));
            OnPropertyChanged(nameof(OpenReferenceMenuText));
            OnPropertyChanged(nameof(OpenAppDataFolderMenuText));
            OnPropertyChanged(nameof(OpenGameFolderMenuText));
            OnPropertyChanged(nameof(ExitMenuText));
            OnPropertyChanged(nameof(EditMenuHeader));
            OnPropertyChanged(nameof(SettingsMenuText));
            OnPropertyChanged(nameof(ProfileMenuHeader));
            OnPropertyChanged(nameof(SwitchProfileMenuText));
            OnPropertyChanged(nameof(ManageProfilesMenuText));
            OnPropertyChanged(nameof(ReferenceHistoryMenuText));
            OnPropertyChanged(nameof(ViewPendingChangesMenuText));
            OnPropertyChanged(nameof(HelpMenuHeader));
            OnPropertyChanged(nameof(CheckForUpdatesMenuText));
            OnPropertyChanged(nameof(AboutMenuText));
            OnPropertyChanged(nameof(DebugMenuHeader));
            OnPropertyChanged(nameof(ResetConfigMenuText));
            OnPropertyChanged(nameof(OpenConfigFolderMenuText));
            OnPropertyChanged(nameof(ThrowTestExceptionMenuText));
            OnPropertyChanged(nameof(DownloadOptionsButtonText));
            OnPropertyChanged(nameof(ActiveProfilePrefixText));
            OnPropertyChanged(nameof(RecentStatusMessagesText));
            OnPropertyChanged(nameof(PluginsModifiedWarningText));
        }
    }
}
