using System;
using System.Diagnostics;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoadOrderKeeper.Services;

namespace LoadOrderKeeper.ViewModels
{
    public partial class AboutViewModel : ObservableObject
    {
        public string ApplicationName { get; } = "Starfield Load Order Keeper";
        public string ApplicationVersion { get; }
        public string Copyright { get; }
        public string Description { get; } = "A tool to help manage and maintain your Starfield mods load order once you have started a game.";
        public string HomepageUrl { get; } = "https://github.com/Mistralys/starfield-load-order-manager";
        
        // Button labels
        public string HomepageButtonText { get; } = "Homepage";
        public string CloseButtonText { get; } = "Close";
        public string VersionLabelText { get; } = "Version ";

        public AboutViewModel()
        {
            ApplicationVersion = VersionService.GetApplicationVersion();
            Copyright = $"© 2025-{DateTime.Now.Year} Mistralys";
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