using LoadOrderKeeper.Services;

namespace LoadOrderKeeper.ViewTexts
{
    /// <summary>
    /// ViewModel for menu and UI text properties.
    /// Centralizes all static text strings for menus, labels, and buttons.
    /// </summary>
    public class MenuViewModel
    {
        // Window Title
        public string WindowTitle => string.Format(Resources.MainWindowResources.WindowTitleFormat, VersionService.GetApplicationVersion());

        // File Menu
        public string FileMenuHeader => Resources.MainWindowResources.FileMenuHeader;
        public string OpenPluginsMenuText => Resources.MainWindowResources.OpenPluginsMenuText;
        public string OpenReferenceMenuText => Resources.MainWindowResources.OpenReferenceMenuText;
        public string OpenAppDataFolderMenuText => Resources.MainWindowResources.OpenAppDataFolderMenuText;
        public string OpenGameFolderMenuText => Resources.MainWindowResources.OpenGameFolderMenuText;
        public string ExitMenuText => Resources.MainWindowResources.ExitMenuText;

        // Edit Menu
        public string EditMenuHeader { get; } = Resources.MainWindowResources.EditMenuHeader;
        public string SettingsMenuText { get; } = Resources.MainWindowResources.SettingsMenuText;

        // Profile Menu
        public string ProfileMenuHeader { get; } = Resources.MainWindowResources.ProfileMenuHeader;
        public string SwitchProfileMenuText { get; } = Resources.MainWindowResources.SwitchProfileMenuText;
        public string ManageProfilesMenuText { get; } = Resources.MainWindowResources.ManageProfilesMenuText;
        public string ReferenceHistoryMenuText { get; } = Resources.MainWindowResources.ReferenceHistoryMenuText;
        public string ViewPendingChangesMenuText { get; } = Resources.MainWindowResources.ViewPendingChangesMenuText;

        // Help Menu
        public string HelpMenuHeader { get; } = Resources.MainWindowResources.HelpMenuHeader;
        public string CheckForUpdatesMenuText { get; } = Resources.MainWindowResources.CheckForUpdatesMenuText;
        public string AboutMenuText { get; } = Resources.MainWindowResources.AboutMenuText;

        // Debug Menu
        public string DebugMenuHeader { get; } = Resources.MainWindowResources.DebugMenuHeader;
        public string ResetConfigMenuText { get; } = Resources.MainWindowResources.ResetConfigMenuText;
        public string OpenConfigFolderMenuText { get; } = Resources.MainWindowResources.OpenConfigFolderMenuText;
        public string ThrowTestExceptionMenuText { get; } = Resources.MainWindowResources.ThrowTestExceptionMenuText;

        // Button Text
        public string DownloadOptionsButtonText { get; } = Resources.MainWindowResources.DownloadOptionsButtonText;

        // Labels
        public string ActiveProfilePrefixText { get; } = Resources.MainWindowResources.ActiveProfilePrefixText;
        public string RecentStatusMessagesText { get; } = Resources.MainWindowResources.RecentStatusMessagesText;

        // Warning Messages
        public string PluginsModifiedWarningText { get; } = Resources.MainWindowResources.PluginsModifiedWarningText;
    }
}
