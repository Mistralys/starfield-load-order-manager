using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LoadOrderKeeper.Coordinators;
using LoadOrderKeeper.Models;

namespace LoadOrderKeeper.Services
{
    /// <summary>
    /// Service for capturing and exporting debug state of the application.
    /// </summary>
    public static class DebugStateService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
        };

        /// <summary>
        /// Captures the current application state and serializes it to JSON.
        /// Paths are sanitized to replace user-specific information with placeholders.
        /// Works even when configuration is invalid.
        /// </summary>
        public static async Task<string> CaptureDebugStateAsync(
            AppConfigModel config,
            IReadOnlyList<DiffLineModel> changeList,
            ConfigurationCoordinator? configCoordinator = null,
            StatusCoordinator? statusCoordinator = null)
        {
            // Get validation result if coordinator provided
            ValidationResult validationResult;
            if (configCoordinator != null)
            {
                validationResult = configCoordinator.GetValidationResult();
            }
            else
            {
                // Fallback to basic validation if no coordinator provided
                validationResult = config.IsValid() 
                    ? ValidationResult.Success() 
                    : ValidationResult.Failed("Configuration validation not available");
            }

            var debugState = new DebugStateModel
            {
                ApplicationVersion = VersionService.GetApplicationVersion(),
                Configuration = new DebugStateModel.ConfigurationState
                {
                    AppDataPath = SanitizePath(config.StarfieldAppDataPath),
                    GamePath = SanitizePath(config.StarfieldGamePath),
                    ActiveProfileId = config.ActiveProfileId,
                    IsValid = validationResult.IsValid,
                    ValidationError = validationResult.ErrorMessage
                },
                Steam = new DebugStateModel.SteamState
                {
                    IsInstalled = SettingsService.IsStarfieldInstalledViaSteam(),
                    IsRunning = SettingsService.IsSteamRunning()
                },
                TotalChangesDetected = changeList.Count,
                PluginsTxtContents = await ReadFileContentsSafeAsync(config.GetPluginsFilePath()),
                ReferenceContents = await ReadFileContentsSafeAsync(config.GetReferenceFilePath()),
                ChangeList = changeList.ToList(),
                StatusMessages = statusCoordinator?.GetAllMessages().ToList() ?? new List<StatusMessageModel>()
            };

            return JsonSerializer.Serialize(debugState, JsonOptions);
        }

        /// <summary>
        /// Sanitizes file paths by replacing user-specific segments with placeholders.
        /// Replaces user profile path (C:\Users\username) with %USERPROFILE%.
        /// </summary>
        private static string SanitizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return path;
            }

            // Get the user profile path (e.g., C:\Users\username)
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            
            if (string.IsNullOrWhiteSpace(userProfile))
            {
                return path;
            }

            // Replace with placeholder
            if (path.StartsWith(userProfile, StringComparison.OrdinalIgnoreCase))
            {
                return "%USERPROFILE%" + path.Substring(userProfile.Length);
            }

            return path;
        }

        /// <summary>
        /// Reads file contents as a list of lines, or returns empty list if file doesn't exist or cannot be read.
        /// Safe version that never throws exceptions.
        /// </summary>
        private static async Task<List<string>> ReadFileContentsSafeAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return new List<string>();
            }

            if (!File.Exists(filePath))
            {
                return new List<string>();
            }

            try
            {
                var lines = await File.ReadAllLinesAsync(filePath, Encoding.UTF8);
                return lines.ToList();
            }
            catch
            {
                // If reading fails, return empty list
                return new List<string>();
            }
        }
    }
}
