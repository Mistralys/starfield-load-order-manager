using System;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoadOrderKeeper.Services;

namespace LoadOrderKeeper.ViewModels
{
    /// <summary>
    /// ViewModel for the error dialog displayed when unhandled exceptions occur.
    /// </summary>
    public partial class ErrorDialogViewModel : ObservableObject
    {
        private const string BugReportUrl = "https://github.com/Mistralys/starfield-load-order-manager/issues";

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private string _errorDetails = string.Empty;

        public event EventHandler? CloseRequested;
        public event EventHandler? ExitRequested;

        public ErrorDialogViewModel(Exception exception)
        {
            _errorMessage = exception.Message;
            _errorDetails = $"{exception.GetType().Name}: {exception.Message}";
        }

        /// <summary>
        /// Opens the application data folder in File Explorer where the error.log file is located.
        /// </summary>
        [RelayCommand]
        private void OpenLogFolder()
        {
            try
            {
                var folderPath = SettingsService.GetConfigFolderPath();
                
                if (Directory.Exists(folderPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = folderPath,
                        UseShellExecute = true,
                        Verb = "open"
                    });
                }
            }
            catch
            {
                // Silently fail - user can still report the bug or exit
            }
        }

        /// <summary>
        /// Opens the GitHub issues page in the default browser.
        /// </summary>
        [RelayCommand]
        private void ReportBug()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = BugReportUrl,
                    UseShellExecute = true
                });
            }
            catch
            {
                // Silently fail - user can manually navigate to the URL
            }
        }

        /// <summary>
        /// Exits the application immediately.
        /// </summary>
        [RelayCommand]
        private void Exit()
        {
            ExitRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Dismisses the error dialog and attempts to continue running (unsafe).
        /// </summary>
        [RelayCommand]
        private void Ignore()
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
