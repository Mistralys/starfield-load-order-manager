using System;
using System.Collections.Generic;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoadOrderKeeper.Models;
using LoadOrderKeeper.Services;

namespace LoadOrderKeeper.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _starfieldAppDataPath = SettingsService.TryGetDefaultAppDataPath();

        [ObservableProperty]
        private string _starfieldGamePath = SettingsService.TryGetDefaultSteamPath();

        [ObservableProperty]
        private bool _statusBannerVisible = true;

        [ObservableProperty]
        private string _statusBannerMessage = string.Empty;

        [ObservableProperty]
        private bool _statusBannerIsError = true;

        public string DetectedAppDataPath { get; }
        public string DetectedGamePath { get; }
        public bool HasDetectedAppDataPath => !string.IsNullOrWhiteSpace(DetectedAppDataPath);
        public bool HasDetectedGamePath => !string.IsNullOrWhiteSpace(DetectedGamePath);

        public event EventHandler? BrowseAppDataRequested;
        public event EventHandler? BrowseGamePathRequested;
        public event EventHandler? SaveRequested;

        public SettingsViewModel(AppConfigModel initialConfig)
        {
            DetectedAppDataPath = SettingsService.TryGetDefaultAppDataPath();
            DetectedGamePath = SettingsService.TryGetDefaultSteamPath();

            if (!string.IsNullOrWhiteSpace(initialConfig.StarfieldAppDataPath))
            {
                StarfieldAppDataPath = initialConfig.StarfieldAppDataPath;
            }

            if (!string.IsNullOrWhiteSpace(initialConfig.StarfieldGamePath))
            {
                StarfieldGamePath = initialConfig.StarfieldGamePath;
            }

            // Initial validation
            ValidateConfiguration();
        }

        [RelayCommand]
        private void BrowseAppData()
        {
            BrowseAppDataRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void BrowseGamePath()
        {
            BrowseGamePathRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void SaveSettings()
        {
            SaveRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void UseDetectedAppDataPath()
        {
            if (HasDetectedAppDataPath)
            {
                StarfieldAppDataPath = DetectedAppDataPath;
                ValidateConfiguration(); // Update status banner immediately
            }
        }

        [RelayCommand]
        private void UseDetectedGamePath()
        {
            if (HasDetectedGamePath)
            {
                StarfieldGamePath = DetectedGamePath;
                ValidateConfiguration(); // Update status banner immediately
            }
        }

        public void UpdateAppDataPath(string selectedPath)
        {
            if (!string.IsNullOrWhiteSpace(selectedPath))
            {
                StarfieldAppDataPath = selectedPath;
            }
        }

        public void UpdateGamePath(string selectedPath)
        {
            if (!string.IsNullOrWhiteSpace(selectedPath))
            {
                StarfieldGamePath = selectedPath;
            }
        }

        public AppConfigModel GetConfig()
        {
            return new AppConfigModel
            {
                StarfieldAppDataPath = StarfieldAppDataPath,
                StarfieldGamePath = StarfieldGamePath
            };
        }

        /// <summary>
        /// Validates the current configuration and updates the status banner.
        /// Called on window load, path changes (blur), and save button click.
        /// </summary>
        public void ValidateConfiguration()
        {
            var errors = new List<string>();

            // Check AppData path
            bool appDataPathValid = !string.IsNullOrWhiteSpace(StarfieldAppDataPath) && 
                                   Directory.Exists(StarfieldAppDataPath);
            if (!appDataPathValid)
            {
                if (string.IsNullOrWhiteSpace(StarfieldAppDataPath))
                {
                    errors.Add("The app data path is not configured");
                }
                else
                {
                    errors.Add("The app data path is invalid");
                }
            }

            // Check Game path
            bool gamePathValid = !string.IsNullOrWhiteSpace(StarfieldGamePath) && 
                                Directory.Exists(StarfieldGamePath);
            bool dataFolderValid = gamePathValid && 
                                  Directory.Exists(Path.Combine(StarfieldGamePath, "Data"));
            
            if (!gamePathValid)
            {
                if (string.IsNullOrWhiteSpace(StarfieldGamePath))
                {
                    errors.Add("The game path is not configured");
                }
                else
                {
                    errors.Add("The game path is invalid");
                }
            }
            else if (!dataFolderValid)
            {
                errors.Add("The game Data folder was not found");
            }

            // Check Plugins.txt if AppData path is valid
            if (errors.Count == 0 && appDataPathValid)
            {
                var pluginsPath = Path.Combine(StarfieldAppDataPath, "Plugins.txt");
                if (!File.Exists(pluginsPath))
                {
                    errors.Add("Plugins.txt not found in the app data folder");
                }
            }

            // Check Profiles folder if paths are valid and Plugins.txt exists
            if (errors.Count == 0 && appDataPathValid)
            {
                var profilesFolder = Path.Combine(StarfieldAppDataPath, "Profiles");
                try
                {
                    if (!Directory.Exists(profilesFolder))
                    {
                        Directory.CreateDirectory(profilesFolder);
                    }
                    
                    // Test writability
                    var testFile = Path.Combine(profilesFolder, $".test_{Guid.NewGuid():N}");
                    File.WriteAllText(testFile, "test");
                    File.Delete(testFile);
                }
                catch (UnauthorizedAccessException)
                {
                    errors.Add("Access denied when creating the Profiles folder");
                }
                catch
                {
                    errors.Add("The Profiles folder cannot be created or accessed");
                }
            }

            // Update banner based on validation results
            StatusBannerVisible = true;
            
            if (errors.Count > 0)
            {
                StatusBannerIsError = true;
                
                if (errors.Count == 1)
                {
                    StatusBannerMessage = errors[0] + ".";
                }
                else if (errors.Count == 2 && errors.Contains("The app data path is invalid") && errors.Contains("The game path is invalid"))
                {
                    StatusBannerMessage = "Both the game path and app data path are invalid.";
                }
                else
                {
                    StatusBannerMessage = string.Join(". ", errors) + ".";
                }
            }
            else
            {
                StatusBannerIsError = false;
                StatusBannerMessage = "The configured paths are valid.";
            }
        }
    }
}
