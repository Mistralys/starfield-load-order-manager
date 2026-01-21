using System;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoadOrderKeeper.Services;

namespace LoadOrderKeeper.ViewTexts
{
    public partial class AboutViewModel : ObservableObject
    {
        private readonly LocalizationService _localization = LocalizationService.Instance;

        public string ApplicationName => _localization.GetString("About", "ApplicationName");
        public string Description => _localization.GetString("About", "Description");
        public string HomepageButtonText => _localization.GetString("About", "HomepageButtonText");
        public string CloseButtonText => _localization.GetString("About", "CloseButtonText");
        public string VersionLabelText => _localization.GetString("About", "VersionLabelText");
        
        public string ApplicationVersion { get; }
        public string Copyright { get; private set; }
        public string HomepageUrl { get; } = "https://github.com/Mistralys/starfield-load-order-manager";

        public AboutViewModel()
        {
            ApplicationVersion = VersionService.GetApplicationVersion();
            Copyright = _localization.GetString("About", "CopyrightFormat", DateTime.Now.Year);
            
            // Subscribe to culture changes
            _localization.CultureChanged += OnCultureChanged;
        }

        private void OnCultureChanged(object? sender, EventArgs e)
        {
            // Refresh all localized strings
            OnPropertyChanged(nameof(ApplicationName));
            OnPropertyChanged(nameof(Description));
            OnPropertyChanged(nameof(HomepageButtonText));
            OnPropertyChanged(nameof(CloseButtonText));
            OnPropertyChanged(nameof(VersionLabelText));
            // Update copyright with new culture formatting
            Copyright = _localization.GetString("About", "CopyrightFormat", DateTime.Now.Year);
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
                System.Diagnostics.Debug.WriteLine($"Failed to open homepage: {ex.Message}");
            }
        }

        public event EventHandler? CloseRequested;
    }
}