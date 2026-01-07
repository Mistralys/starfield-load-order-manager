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
    }
}
