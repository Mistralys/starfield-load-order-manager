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
        private readonly Func<bool, string> _getReadyStatusMessage;
        private readonly Action<AppConfigModel> _updateCoordinators;
        private readonly ViewTexts.ViewModelInitializerStatusTexts _statusTexts = new();

        public ViewModelInitializer(
            Action<string, StatusMessageType> addStatusMessage,
            Func<bool, string> getReadyStatusMessage,
            Action<AppConfigModel> updateCoordinators)
        {
            _addStatusMessage = addStatusMessage;
            _getReadyStatusMessage = getReadyStatusMessage;
            _updateCoordinators = updateCoordinators;
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

            // Initialize localization with preferred language from config
            ViewTexts.LocalizationService.Instance.InitializeFromConfig(config.PreferredLanguage);

            // IMPORTANT: Ensure Profiles folder and default profile files exist BEFORE validation
            // This prevents a false "invalid config" message on startup when the folder doesn't exist yet
            if (config.IsValid())
            {
                try
                {
                    ProfileService.EnsureProfilesFolderExists(config);
                    await ProfileService.EnsureDefaultProfileFilesAsync(config);
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

            // Update coordinator configurations - this triggers validation
            // At this point, the Profiles folder should already exist if config is valid
            _updateCoordinators(config);

            bool refExists = FileService.DoesReferenceFileExist(config);

            var referenceResult = await EnsureReferenceFileExistsAsync(config, refExists);
            if (referenceResult == ReferenceInitializationResult.AlreadyExists)
            {
                // Pass the actual config validity instead of relying on MainViewModel's Config property
                // which hasn't been updated yet at this point in initialization
                _addStatusMessage(_getReadyStatusMessage(config.IsValid()), StatusMessageType.Info);
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
                _addStatusMessage(string.Format(_statusTexts.PluginsTxtNotFoundFormat, pluginsPath), StatusMessageType.Warning);
                return ReferenceInitializationResult.MissingPluginsFile;
            }

            try
            {
                _addStatusMessage(_statusTexts.NoReferenceCreating, StatusMessageType.Info);
                await FileService.CreateReferenceFileAsync(config);
                _addStatusMessage(_statusTexts.ReferenceCreatedAuto, StatusMessageType.Success);
                return ReferenceInitializationResult.Created;
            }
            catch (Exception ex)
            {
                _addStatusMessage(string.Format(_statusTexts.FailedToCreateReferenceFormat, ex.Message), StatusMessageType.Error);
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
