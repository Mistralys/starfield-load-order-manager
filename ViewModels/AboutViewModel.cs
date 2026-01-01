using System;
using System.Diagnostics;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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
            ApplicationVersion = GetApplicationVersion();
            Copyright = $"© 2025-{DateTime.Now.Year} Mistralys";
        }

        /// <summary>
        /// Gets the application version from assembly attributes.
        /// </summary>
        /// <returns>The version string, preferring InformationalVersion over AssemblyVersion.</returns>
        private string GetApplicationVersion()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                
                // Try to get the InformationalVersion first (this contains the original Git tag)
                var informationalVersionAttribute = assembly
                    .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
                    .FirstOrDefault() as System.Reflection.AssemblyInformationalVersionAttribute;
                
                if (informationalVersionAttribute?.InformationalVersion is not null && 
                    !string.IsNullOrWhiteSpace(informationalVersionAttribute.InformationalVersion))
                {
                    return informationalVersionAttribute.InformationalVersion;
                }
                
                // Fallback to AssemblyVersion
                var assemblyVersion = assembly.GetName().Version;
                if (assemblyVersion != null)
                {
                    return assemblyVersion.ToString();
                }
                
                // Last resort fallback
                return "Unknown";
            }
            catch
            {
                // If anything goes wrong, return a safe fallback
                return "Unknown";
            }
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