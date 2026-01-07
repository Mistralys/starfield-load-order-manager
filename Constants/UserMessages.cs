namespace LoadOrderKeeper.Constants
{
    /// <summary>
    /// Centralized user-facing messages for consistent communication and easy localization.
    /// </summary>
    public static class UserMessages
    {
        /// <summary>
        /// Guidance message to append to error messages when configuration is invalid.
        /// Directs the user to check the main window for the configuration error banner.
        /// </summary>
        public const string ConfigInvalidGuidance = 
            "\n\nThe likely cause is that the current configuration is invalid. Please refer to the error message in the main window to fix this.";
        
        /// <summary>
        /// Error message when the Profiles folder cannot be created or accessed.
        /// Provides actionable guidance to the user.
        /// </summary>
        public const string ProfilesFolderRequired = 
            "The application requires a 'Profiles' folder in your configured app data path to store profile data. " +
            "This folder could not be created or accessed. Please check folder permissions or select a different app data path in settings.";
        
        /// <summary>
        /// Error message specifically for access denied errors on the Profiles folder.
        /// </summary>
        public const string ProfilesFolderAccessDenied = 
            "Access denied when creating the Profiles folder. You may need administrator rights or to choose a different location.";
        
        /// <summary>
        /// Error message when Plugins.txt is not found in the app data folder.
        /// Directs the user to run Starfield to generate the file.
        /// </summary>
        public const string PluginsTxtRequired = 
            "The Plugins.txt file was not found in the configured app data path. " +
            "This file is required for the application to function. " +
            "Please ensure you have run Starfield at least once to generate this file, or select the correct app data folder in settings.";
    }
}
