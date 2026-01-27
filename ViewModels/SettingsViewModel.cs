using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoadOrderKeeper.Models;
using LoadOrderKeeper.Services;
using LoadOrderKeeper.ViewTexts;

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

        [ObservableProperty]
        private string _selectedLanguage = "auto";

        [ObservableProperty]
        private bool _languageChanged;

        [ObservableProperty]
        private string _customEditorPath = string.Empty;

        private readonly string? _activeProfileId;

        public string DetectedAppDataPath { get; }
        public string DetectedGamePath { get; }
        public bool HasDetectedAppDataPath => !string.IsNullOrWhiteSpace(DetectedAppDataPath);
        public bool HasDetectedGamePath => !string.IsNullOrWhiteSpace(DetectedGamePath);
        
        public List<LanguageOption> AvailableLanguages { get; }
        
        // Localized texts for UI bindings
        public SettingsWindowTexts Texts { get; } = new();

        public event EventHandler? BrowseAppDataRequested;
        public event EventHandler? BrowseGamePathRequested;
        public event EventHandler? BrowseCustomEditorRequested;
        public event EventHandler? SaveRequested;

        public SettingsViewModel(AppConfigModel initialConfig)
        {
            DetectedAppDataPath = SettingsService.TryGetDefaultAppDataPath();
            DetectedGamePath = SettingsService.TryGetDefaultSteamPath();

            // Preserve the active profile ID from the initial config
            _activeProfileId = initialConfig.ActiveProfileId;

            if (!string.IsNullOrWhiteSpace(initialConfig.StarfieldAppDataPath))
            {
                StarfieldAppDataPath = initialConfig.StarfieldAppDataPath;
            }

            if (!string.IsNullOrWhiteSpace(initialConfig.StarfieldGamePath))
            {
                StarfieldGamePath = initialConfig.StarfieldGamePath;
            }

            // Initialize custom editor path
            CustomEditorPath = initialConfig.CustomEditorPath ?? string.Empty;

            // Initialize language selection
            SelectedLanguage = initialConfig.PreferredLanguage ?? "auto";
            AvailableLanguages = BuildLanguageList();

            // Initial validation
            ValidateConfiguration();
        }

        private List<LanguageOption> BuildLanguageList()
        {
            var locService = ViewTexts.LocalizationService.Instance;
            var settingsTexts = new SettingsWindowTexts();
            
            var languages = new List<LanguageOption>
            {
                new LanguageOption("auto", settingsTexts.LanguageAutomatic)
            };

            // Get available cultures from the localization service
            var availableCultures = locService.GetAvailableCultures();
            
            foreach (var culture in availableCultures)
            {
                // Read display name from locale file (100% dynamic)
                var displayName = locService.GetLocaleName(culture);
                languages.Add(new LanguageOption(culture, displayName));
            }

            return languages;
        }

        partial void OnSelectedLanguageChanged(string value)
        {
            // Check if the selected language differs from the session start culture
            // SessionStartCulture is set once when the app starts and persists for the entire session
            var locService = ViewTexts.LocalizationService.Instance;
            LanguageChanged = value != locService.SessionStartCulture;
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
        private void BrowseCustomEditor()
        {
            BrowseCustomEditorRequested?.Invoke(this, EventArgs.Empty);
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

        public void UpdateCustomEditorPath(string selectedPath)
        {
            if (!string.IsNullOrWhiteSpace(selectedPath))
            {
                CustomEditorPath = selectedPath;
            }
        }

        public AppConfigModel GetConfig()
        {
            return new AppConfigModel
            {
                StarfieldAppDataPath = StarfieldAppDataPath,
                StarfieldGamePath = StarfieldGamePath,
                PreferredLanguage = SelectedLanguage,
                CustomEditorPath = string.IsNullOrWhiteSpace(CustomEditorPath) ? null : CustomEditorPath,
                ActiveProfileId = _activeProfileId
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
                    errors.Add(Texts.AppDataNotConfiguredMessage);
                }
                else
                {
                    errors.Add(Texts.InvalidAppDataMessage);
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
                    errors.Add(Texts.GamePathNotConfiguredMessage);
                }
                else
                {
                    errors.Add(Texts.InvalidGamePathMessage);
                }
            }
            else if (!dataFolderValid)
            {
                errors.Add(Texts.GameDataFolderNotFoundMessage);
            }

            // Check Plugins.txt if AppData path is valid
            if (errors.Count == 0 && appDataPathValid)
            {
                var pluginsPath = Path.Combine(StarfieldAppDataPath, "Plugins.txt");
                if (!File.Exists(pluginsPath))
                {
                    errors.Add(Texts.PluginsTxtNotFoundMessage);
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
                    errors.Add(Texts.ProfilesFolderAccessDeniedMessage);
                }
                catch
                {
                    errors.Add(Texts.ProfilesFolderCannotBeCreatedMessage);
                }
            }

            // Validate custom editor path if provided
            if (!string.IsNullOrWhiteSpace(CustomEditorPath))
            {
                if (!File.Exists(CustomEditorPath))
                {
                    errors.Add(Texts.CustomEditorNotFoundError);
                }
                else if (!CustomEditorPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add(Texts.CustomEditorInvalidPathError);
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
                else if (errors.Count == 2 && errors.Contains(Texts.InvalidAppDataMessage) && errors.Contains(Texts.InvalidGamePathMessage))
                {
                    StatusBannerMessage = Texts.BothPathsInvalidMessage;
                }
                else
                {
                    StatusBannerMessage = string.Join(". ", errors) + ".";
                }
            }
            else
            {
                StatusBannerIsError = false;
                StatusBannerMessage = Texts.ValidConfigMessage;
            }
        }
    }
}
