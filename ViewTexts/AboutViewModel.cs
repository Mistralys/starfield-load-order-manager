using System;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoadOrderKeeper.Services;
using FlowDirection = System.Windows.FlowDirection;

namespace LoadOrderKeeper.ViewTexts
{
    public partial class AboutViewModel : ObservableObject
    {
        public string ApplicationName => LoadOrderKeeper.Resources.AboutWindowResources.ApplicationName;
        public string Description => LoadOrderKeeper.Resources.AboutWindowResources.Description;
        public string HomepageButtonText => LoadOrderKeeper.Resources.AboutWindowResources.HomepageButtonText;
        public string CloseButtonText => LoadOrderKeeper.Resources.AboutWindowResources.CloseButtonText;
        public string VersionLabelText => LoadOrderKeeper.Resources.AboutWindowResources.VersionLabelText;
        
        public string ApplicationVersion { get; }
        public string Copyright { get; private set; }
        public string HomepageUrl { get; } = "https://github.com/Mistralys/starfield-load-order-manager";
        
        // Flow direction for RTL support (prepared for future)
        public FlowDirection FlowDirection => App.LocalizationService.CurrentFlowDirection;

        public AboutViewModel()
        {
            ApplicationVersion = VersionService.GetApplicationVersion();
            Copyright = string.Format(LoadOrderKeeper.Resources.AboutWindowResources.CopyrightFormat, DateTime.Now.Year);
            
            // Debug: Log current culture information
            System.Diagnostics.Debug.WriteLine($"[AboutViewModel] Current UI Culture: {System.Globalization.CultureInfo.CurrentUICulture.Name}");
            System.Diagnostics.Debug.WriteLine($"[AboutViewModel] Current Culture: {System.Globalization.CultureInfo.CurrentCulture.Name}");
            System.Diagnostics.Debug.WriteLine($"[AboutViewModel] LocalizationService Culture: {App.LocalizationService.CurrentCulture.Name}");
            System.Diagnostics.Debug.WriteLine($"[AboutViewModel] ApplicationName from resources: {LoadOrderKeeper.Resources.AboutWindowResources.ApplicationName}");
            
            // Subscribe to culture changes to update FlowDirection
            App.LocalizationService.CultureChanged += OnCultureChanged;
        }

        private void OnCultureChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(FlowDirection));
            // Refresh all localized strings
            OnPropertyChanged(nameof(ApplicationName));
            OnPropertyChanged(nameof(Description));
            OnPropertyChanged(nameof(HomepageButtonText));
            OnPropertyChanged(nameof(CloseButtonText));
            OnPropertyChanged(nameof(VersionLabelText));
            // Update copyright with new culture formatting
            Copyright = string.Format(LoadOrderKeeper.Resources.AboutWindowResources.CopyrightFormat, DateTime.Now.Year);
            OnPropertyChanged(nameof(Copyright));
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