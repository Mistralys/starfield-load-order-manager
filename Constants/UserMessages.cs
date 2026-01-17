using LoadOrderKeeper.Resources;

namespace LoadOrderKeeper.Constants
{
    /// <summary>
    /// Centralized user-facing messages for consistent communication and easy localization.
    /// This class now serves as a facade over the CommonResources resource file.
    /// </summary>
    public static class UserMessages
    {
        /// <summary>
        /// Guidance message to append to error messages when configuration is invalid.
        /// Directs the user to check the main window for the configuration error banner.
        /// </summary>
        public static string ConfigInvalidGuidance => LoadOrderKeeper.Resources.CommonResources.ConfigInvalidGuidance;
        
        /// <summary>
        /// Error message when the Profiles folder cannot be created or accessed.
        /// Provides actionable guidance to the user.
        /// </summary>
        public static string ProfilesFolderRequired => LoadOrderKeeper.Resources.CommonResources.ProfilesFolderRequired;
        
        /// <summary>
        /// Error message specifically for access denied errors on the Profiles folder.
        /// </summary>
        public static string ProfilesFolderAccessDenied => LoadOrderKeeper.Resources.CommonResources.ProfilesFolderAccessDenied;
        
        /// <summary>
        /// Error message when Plugins.txt is not found in the app data folder.
        /// Directs the user to run Starfield to generate the file.
        /// </summary>
        public static string PluginsTxtRequired => LoadOrderKeeper.Resources.CommonResources.PluginsTxtRequired;
        
        /// <summary>
        /// Format string for error messages. {0} is the error details.
        /// </summary>
        public static string ErrorPrefix => LoadOrderKeeper.Resources.CommonResources.ErrorPrefix;
    }
}
