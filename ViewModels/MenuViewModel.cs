using LoadOrderKeeper.Services;

namespace LoadOrderKeeper.ViewModels
{
    /// <summary>
    /// ViewModel for menu and UI text properties.
    /// Centralizes all static text strings for menus, labels, and buttons.
    /// </summary>
    public class MenuViewModel
    {
        // Window Title
        public string WindowTitle => $"Starfield Load Order Keeper v{VersionService.GetApplicationVersion()}";

        // File Menu
        public string FileMenuHeader { get; } = "_File";
        public string OpenPluginsMenuText { get; } = "Open _Plugins.txt";
        public string OpenReferenceMenuText { get; } = "Open _Reference File";
        public string OpenAppDataFolderMenuText { get; } = "Open _AppData Folder";
        public string OpenGameFolderMenuText { get; } = "Open _Game Folder";
        public string ExitMenuText { get; } = "E_xit";

        // Edit Menu
        public string EditMenuHeader { get; } = "_Edit";
        public string SettingsMenuText { get; } = "_Settings...";

        // Profile Menu
        public string ProfileMenuHeader { get; } = "_Profile";
        public string SwitchProfileMenuText { get; } = "_Switch Profile...";
        public string ManageProfilesMenuText { get; } = "_Manage Profiles...";
        public string ReferenceHistoryMenuText { get; } = "History of changes...";
        public string ViewPendingChangesMenuText { get; } = "_View Pending Changes...";

        // Help Menu
        public string HelpMenuHeader { get; } = "_Help";
        public string CheckForUpdatesMenuText { get; } = "Check for _Updates...";
        public string AboutMenuText { get; } = "_About...";

        // Debug Menu
        public string DebugMenuHeader { get; } = "_Debug";
        public string ResetConfigMenuText { get; } = "_Reset Configuration (for testing)...";
        public string OpenConfigFolderMenuText { get; } = "Open _Configuration Folder";
        public string ThrowTestExceptionMenuText { get; } = "_Throw Test Exception";

        // Button Text
        public string DownloadOptionsButtonText { get; } = "Download options...";

        // Labels
        public string CurrentTargetLabel { get; } = "Current Plugins.txt target:";
        public string TargetPrefixText { get; } = "Target: ";
        public string ActiveProfilePrefixText { get; } = "Active Profile: ";
        public string RecentStatusMessagesText { get; } = "Recent Status Messages:";

        // Warning Messages
        public string PluginsModifiedWarningText { get; } = "Plugins.txt was modified outside Load Order Keeper.";
    }
}
