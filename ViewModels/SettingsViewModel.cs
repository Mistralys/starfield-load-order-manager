using System;
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

        public event EventHandler? BrowseAppDataRequested;
        public event EventHandler? BrowseGamePathRequested;
        public event EventHandler? SaveRequested;

        public SettingsViewModel(AppConfigModel initialConfig)
        {
            if (!string.IsNullOrWhiteSpace(initialConfig.StarfieldAppDataPath))
            {
                StarfieldAppDataPath = initialConfig.StarfieldAppDataPath;
            }

            if (!string.IsNullOrWhiteSpace(initialConfig.StarfieldGamePath))
            {
                StarfieldGamePath = initialConfig.StarfieldGamePath;
            }
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
    }
}
