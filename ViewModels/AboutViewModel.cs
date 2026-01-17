using System;
using System.Diagnostics;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoadOrderKeeper.Services;
using FlowDirection = System.Windows.FlowDirection;

namespace LoadOrderKeeper.ViewModels
{
    public partial class AboutViewModel : ObservableObject
    {
        // TODO: Uncomment these once AboutWindowResources.resx Designer.cs is regenerated with public properties
        // public string ApplicationName => LoadOrderKeeper.Resources.AboutWindowResources.ApplicationName;
        // public string Description => LoadOrderKeeper.Resources.AboutWindowResources.Description;
        // public string HomepageButtonText => LoadOrderKeeper.Resources.AboutWindowResources.HomepageButtonText;
        // public string CloseButtonText => LoadOrderKeeper.Resources.AboutWindowResources.CloseButtonText;
        // public string VersionLabelText => LoadOrderKeeper.Resources.AboutWindowResources.VersionLabelText;
        
        // Temporary: Keep original values until Designer.cs is regenerated
        public string ApplicationName { get; } = "Starfield Load Order Keeper";
        public string Description { get; } = "A tool to help manage and maintain your Starfield mods load order once you have started a game.";
        public string HomepageButtonText { get; } = "Homepage";
        public string CloseButtonText { get; } = "Close";
        public string VersionLabelText { get; } = "Version ";
        
        public string ApplicationVersion { get; }
        public string Copyright { get; private set; }
        public string HomepageUrl { get; } = "https://github.com/Mistralys/starfield-load-order-manager";
        
        // Flow direction for RTL support (prepared for future)
        public FlowDirection FlowDirection => App.LocalizationService.CurrentFlowDirection;

        public AboutViewModel()
        {
            ApplicationVersion = VersionService.GetApplicationVersion();
            Copyright = $"© 2025-{DateTime.Now.Year} Mistralys";
            // TODO: Once Designer.cs is regenerated, use: string.Format(LoadOrderKeeper.Resources.AboutWindowResources.CopyrightFormat, DateTime.Now.Year);
            
            // Subscribe to culture changes to update FlowDirection
            App.LocalizationService.CultureChanged += OnCultureChanged;
        }

        private void OnCultureChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(FlowDirection));
            // TODO: Uncomment once Designer.cs is regenerated
            // Refresh all localized strings
            // OnPropertyChanged(nameof(ApplicationName));
            // OnPropertyChanged(nameof(Description));
            // OnPropertyChanged(nameof(HomepageButtonText));
            // OnPropertyChanged(nameof(CloseButtonText));
            // OnPropertyChanged(nameof(VersionLabelText));
            // Update copyright with new culture formatting
            // Copyright = string.Format(LoadOrderKeeper.Resources.AboutWindowResources.CopyrightFormat, DateTime.Now.Year);
            // OnPropertyChanged(nameof(Copyright));
        }

        [RelayCommand]
        private void Close()
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void OpenHomepage()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = HomepageUrl,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                // Could notify about the error, but for now just silently fail
                // In a production app, you might want to show an error message
                System.Diagnostics.Debug.WriteLine($"Failed to open homepage: {ex.Message}");
            }
        }

        public event EventHandler? CloseRequested;
    }
}