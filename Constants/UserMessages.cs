using LoadOrderKeeper.ViewTexts;

namespace LoadOrderKeeper.Constants
{
    /// <summary>
    /// Centralized user-facing messages for consistent communication and easy localization.
    /// This class now serves as a facade over the LocalizationService.
    /// </summary>
    public static class UserMessages
    {
        private static readonly LocalizationService _localization = LocalizationService.Instance;

        /// <summary>
        /// Guidance message to append to error messages when configuration is invalid.
        /// Directs the user to check the main window for the configuration error banner.
        /// </summary>
        public static string ConfigInvalidGuidance => _localization.GetString("Common", "ConfigInvalidGuidance");
        
        /// <summary>
        /// Error message when the Profiles folder cannot be created or accessed.
        /// Provides actionable guidance to the user.
        /// </summary>
        public static string ProfilesFolderRequired => _localization.GetString("Common", "ProfilesFolderRequired");
        
        /// <summary>
        /// Error message specifically for access denied errors on the Profiles folder.
        /// </summary>
        public static string ProfilesFolderAccessDenied => _localization.GetString("Common", "ProfilesFolderAccessDenied");
        
        /// <summary>
        /// Error message when Plugins.txt is not found in the app data folder.
        /// Directs the user to run Starfield to generate the file.
        /// </summary>
        public static string PluginsTxtRequired => _localization.GetString("Common", "PluginsTxtRequired");
        
        /// <summary>
        /// Format string for error messages. {0} is the error details.
        /// </summary>
        public static string ErrorPrefix => _localization.GetString("Common", "ErrorPrefix");
    }
}
