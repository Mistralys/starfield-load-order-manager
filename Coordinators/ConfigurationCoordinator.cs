using System;
using LoadOrderKeeper.Coordinators.Events;
using LoadOrderKeeper.Models;

namespace LoadOrderKeeper.Coordinators
{
    /// <summary>
    /// Coordinates configuration validation and state management.
    /// Provides centralized validation logic with caching and event notification.
    /// </summary>
    public sealed class ConfigurationCoordinator : CoordinatorBase
    {
        private AppConfigModel? _config;
        private bool _isConfigValid;
        private bool _showErrorBanner;

        /// <summary>
        /// Gets whether the current configuration is valid.
        /// </summary>
        public bool IsConfigValid
        {
            get => _isConfigValid;
            private set => SetProperty(ref _isConfigValid, value);
        }

        /// <summary>
        /// Gets whether the configuration error banner should be displayed.
        /// </summary>
        public bool ShowErrorBanner
        {
            get => _showErrorBanner;
            private set => SetProperty(ref _showErrorBanner, value);
        }

        /// <summary>
        /// Raised when the configuration validation state changes.
        /// </summary>
        public event EventHandler<ConfigValidationChangedEventArgs>? ValidationChanged;

        public ConfigurationCoordinator()
        {
            Initialize();
        }

        public override void Initialize()
        {
            // Default to invalid state until configuration is provided
            _isConfigValid = false;
            _showErrorBanner = false;
        }

        /// <summary>
        /// Updates the coordinator with the current configuration and validates it.
        /// </summary>
        /// <param name="config">The application configuration to validate.</param>
        public void UpdateConfiguration(AppConfigModel? config)
        {
            ThrowIfDisposed();

            var wasValid = _isConfigValid;
            _config = config;

            // Validate the configuration
            ValidateConfiguration();

            // Raise event if state changed
            if (wasValid != _isConfigValid)
            {
                ValidationChanged?.Invoke(this, new ConfigValidationChangedEventArgs(wasValid, _isConfigValid));
            }
        }

        /// <summary>
        /// Validates the current configuration and updates state properties.
        /// </summary>
        public void ValidateConfiguration()
        {
            ThrowIfDisposed();

            var wasValid = _isConfigValid;

            if (_config == null)
            {
                IsConfigValid = false;
                ShowErrorBanner = true;
            }
            else
            {
                IsConfigValid = _config.IsValid();
                ShowErrorBanner = !IsConfigValid;
            }

            // Raise event if state changed
            if (wasValid != _isConfigValid)
            {
                ValidationChanged?.Invoke(this, new ConfigValidationChangedEventArgs(wasValid, _isConfigValid));
            }
        }

        /// <summary>
        /// Gets a detailed validation result with specific error information.
        /// </summary>
        /// <returns>A validation result containing error details.</returns>
        public ValidationResult GetValidationResult()
        {
            ThrowIfDisposed();

            if (_config == null)
            {
                return ValidationResult.Failed("Configuration is null.");
            }

            if (string.IsNullOrWhiteSpace(_config.StarfieldAppDataPath))
            {
                return ValidationResult.Failed("AppData path is not configured.");
            }

            if (string.IsNullOrWhiteSpace(_config.StarfieldGamePath))
            {
                return ValidationResult.Failed("Game path is not configured.");
            }

            if (!System.IO.Directory.Exists(_config.StarfieldAppDataPath))
            {
                return ValidationResult.Failed("AppData path does not exist.");
            }

            if (!System.IO.Directory.Exists(_config.StarfieldGamePath))
            {
                return ValidationResult.Failed("Game path does not exist.");
            }

            var dataPath = System.IO.Path.Combine(_config.StarfieldGamePath, "Data");
            if (!System.IO.Directory.Exists(dataPath))
            {
                return ValidationResult.Failed("Game Data folder not found.");
            }

            var pluginsPath = _config.GetPluginsFilePath();
            if (!System.IO.File.Exists(pluginsPath))
            {
                return ValidationResult.Failed("Plugins.txt not found.");
            }

            // Check Profiles folder writability
            var profilesFolder = System.IO.Path.Combine(_config.StarfieldAppDataPath, "Profiles");
            try
            {
                if (!System.IO.Directory.Exists(profilesFolder))
                {
                    System.IO.Directory.CreateDirectory(profilesFolder);
                }

                // Test writability
                var testFile = System.IO.Path.Combine(profilesFolder, $".test_{Guid.NewGuid():N}");
                System.IO.File.WriteAllText(testFile, "test");
                System.IO.File.Delete(testFile);
            }
            catch (UnauthorizedAccessException)
            {
                return ValidationResult.Failed("Access denied to Profiles folder.");
            }
            catch (Exception ex)
            {
                return ValidationResult.Failed($"Profiles folder access error: {ex.Message}");
            }

            return ValidationResult.Success();
        }

        protected override void OnDisposing()
        {
            _config = null;
        }
    }

    /// <summary>
    /// Represents the result of a configuration validation.
    /// </summary>
    public sealed class ValidationResult
    {
        private ValidationResult(bool isValid, string? errorMessage)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
        }

        /// <summary>
        /// Gets whether the validation was successful.
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// Gets the error message if validation failed, or null if successful.
        /// </summary>
        public string? ErrorMessage { get; }

        /// <summary>
        /// Creates a successful validation result.
        /// </summary>
        public static ValidationResult Success() => new(true, null);

        /// <summary>
        /// Creates a failed validation result with an error message.
        /// </summary>
        public static ValidationResult Failed(string errorMessage) => new(false, errorMessage);
    }
}
