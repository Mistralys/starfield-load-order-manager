using System.IO;
using System.Windows;
using LoadOrderKeeper.Coordinators;
using LoadOrderKeeper.Models;
using LoadOrderKeeper.ViewModels;
using LoadOrderKeeper.Views;
using WpfApplication = System.Windows.Application;

namespace LoadOrderKeeper.Services
{
    /// <summary>
    /// Handles MainViewModel initialization sequence.
    /// Manages configuration loading, validation, profile setup, and initial state.
    /// </summary>
    public class ViewModelInitializer
    {
        private readonly Action<string, StatusMessageType> _addStatusMessage;
        private readonly Func<string> _getReadyStatusMessage;
        private readonly Action<AppConfigModel> _updateCoordinators;
        private readonly Func<Task> _showSettingsDialog;

        public ViewModelInitializer(
            Action<string, StatusMessageType> addStatusMessage,
            Func<string> getReadyStatusMessage,
            Action<AppConfigModel> updateCoordinators,
            Func<Task> showSettingsDialog)
        {
            _addStatusMessage = addStatusMessage;
            _getReadyStatusMessage = getReadyStatusMessage;
            _updateCoordinators = updateCoordinators;
            _showSettingsDialog = showSettingsDialog;
        }

        /// <summary>
        /// Loads and initializes the application state.
        /// </summary>
        public async Task<InitializationResult> LoadInitialStateAsync(
            ConfigurationCoordinator configCoordinator,
            ProfileCoordinator profileCoordinator,
            FileMonitoringCoordinator fileMonitor,
            UpdateCheckCoordinator updateCheckCoordinator)
        {
            var config = await SettingsService.LoadSettingsAsync();

            // Update coordinator configurations
            _updateCoordinators(config);

            // Validate configuration early, including Profiles folder
            if (config.IsValid())
            {
                // Ensure Profiles folder exists and is writable
                try
                {
                    ProfileService.EnsureProfilesFolderExists(config);
                }
                catch (IOException ex)
                {
                    // Show error message and offer to open settings, but don't force shutdown
                    ConfirmationDialog.Show(
                        "Profiles Folder Error",
                        $"{ex.Message}\n\n{Constants.UserMessages.ProfilesFolderRequired}",
                        ConfirmationIcon.Error,
                        ConfirmationButton.OK,
                        ConfirmationResult.OK,
                        WpfApplication.Current?.MainWindow);

                    // Configuration is now invalid, error banner will appear automatically
                    // Re-validate configuration to update error state
                    configCoordinator.ValidateConfiguration();
                }
            }

            // Ensure default profile exists
            await ProfileService.EnsureDefaultProfileFilesAsync(config);

            bool refExists = FileService.DoesReferenceFileExist(config);

            await EnsureValidConfigurationAsync(config);

            var referenceResult = await EnsureReferenceFileExistsAsync(config, refExists);
            if (referenceResult == ReferenceInitializationResult.AlreadyExists)
            {
                _addStatusMessage(_getReadyStatusMessage(), StatusMessageType.Info);
            }

            // Initialize profile coordinator with config and load active profile
            profileCoordinator.UpdateConfiguration(config);
            await profileCoordinator.RefreshActiveProfileAsync();

            // Perform immediate initial check to eliminate startup delay
            _ = fileMonitor.CheckPluginsFileAsync();

            // Check for updates in the background
            _ = updateCheckCoordinator.CheckForUpdatesBackgroundAsync();

            return new InitializationResult(config, refExists);
        }

        /// <summary>
        /// Ensures configuration is valid, prompting for settings if needed.
        /// </summary>
        private async Task EnsureValidConfigurationAsync(AppConfigModel config)
        {
            if (config.IsValid())
            {
                return;
            }

            await _showSettingsDialog();
        }

        /// <summary>
        /// Ensures reference file exists, creating it automatically if possible.
        /// </summary>
        private async Task<ReferenceInitializationResult> EnsureReferenceFileExistsAsync(AppConfigModel config, bool refExists)
        {
            if (!config.IsValid())
            {
                return ReferenceInitializationResult.InvalidConfiguration;
            }

            if (refExists)
            {
                return ReferenceInitializationResult.AlreadyExists;
            }

            string pluginsPath = config.GetPluginsFilePath();
            if (!File.Exists(pluginsPath))
            {
                _addStatusMessage($"Plugins.txt not found at {pluginsPath}. Unable to create reference automatically.", StatusMessageType.Warning);
                return ReferenceInitializationResult.MissingPluginsFile;
            }

            try
            {
                _addStatusMessage("No reference file found. Creating one from current Plugins.txt...", StatusMessageType.Info);
                await FileService.CreateReferenceFileAsync(config);
                _addStatusMessage("Reference file created automatically from current Plugins.txt.", StatusMessageType.Success);
                return ReferenceInitializationResult.Created;
            }
            catch (Exception ex)
            {
                _addStatusMessage($"ERROR: Failed to create reference automatically: {ex.Message}", StatusMessageType.Error);
                return ReferenceInitializationResult.Failed;
            }
        }

        /// <summary>
        /// Result of the initialization process.
        /// </summary>
        public class InitializationResult
        {
            public AppConfigModel Config { get; }
            public bool RefExists { get; }

            public InitializationResult(AppConfigModel config, bool refExists)
            {
                Config = config;
                RefExists = refExists;
            }
        }

        /// <summary>
        /// Result of reference file initialization.
        /// </summary>
        private enum ReferenceInitializationResult
        {
            AlreadyExists,
            Created,
            MissingPluginsFile,
            InvalidConfiguration,
            Failed
        }
    }
}
