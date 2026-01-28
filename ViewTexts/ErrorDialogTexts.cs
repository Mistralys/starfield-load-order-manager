using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LoadOrderKeeper.ViewTexts
{
    /// <summary>
    /// Text ViewModel for ErrorDialog.
    /// </summary>
    public partial class ErrorDialogTexts : ObservableObject
    {
        private readonly LocalizationService _localization = LocalizationService.Instance;

        public ErrorDialogTexts()
        {
            // Subscribe to culture changes
            _localization.CultureChanged += OnCultureChanged;
        }

        // Window
        public string WindowTitle => _localization.GetString("ErrorDialog", "WindowTitle");
        public string ErrorHeaderText => _localization.GetString("ErrorDialog", "ErrorHeaderText");
        
        // Buttons
        public string OpenLogFolderButtonText => _localization.GetString("ErrorDialog", "OpenLogFolderButtonText");
        public string OpenLogFolderTooltip => _localization.GetString("ErrorDialog", "OpenLogFolderTooltip");
        public string ReportBugButtonText => _localization.GetString("ErrorDialog", "ReportBugButtonText");
        public string ReportBugTooltip => _localization.GetString("ErrorDialog", "ReportBugTooltip");
        public string ExitButtonText => _localization.GetString("ErrorDialog", "ExitButtonText");
        public string ExitTooltip => _localization.GetString("ErrorDialog", "ExitTooltip");
        public string IgnoreButtonText => _localization.GetString("ErrorDialog", "IgnoreButtonText");
        public string IgnoreTooltip => _localization.GetString("ErrorDialog", "IgnoreTooltip");
        
        // Constants
        public string BugReportUrl => _localization.GetString("ErrorDialog", "BugReportUrl");
        
        // Test Exception
        public string TestExceptionMessage => _localization.GetString("ErrorDialog", "TestExceptionMessage");

        /// <summary>
        /// Handles culture changes by refreshing all properties.
        /// </summary>
        private void OnCultureChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(WindowTitle));
            OnPropertyChanged(nameof(ErrorHeaderText));
            OnPropertyChanged(nameof(OpenLogFolderButtonText));
            OnPropertyChanged(nameof(OpenLogFolderTooltip));
            OnPropertyChanged(nameof(ReportBugButtonText));
            OnPropertyChanged(nameof(ReportBugTooltip));
            OnPropertyChanged(nameof(ExitButtonText));
            OnPropertyChanged(nameof(ExitTooltip));
            OnPropertyChanged(nameof(IgnoreButtonText));
            OnPropertyChanged(nameof(IgnoreTooltip));
            OnPropertyChanged(nameof(BugReportUrl));
            OnPropertyChanged(nameof(TestExceptionMessage));
        }

        /// <summary>
        /// Refreshes all localized properties when culture changes (legacy compatibility).
        /// </summary>
        public void RefreshAll()
        {
            OnCultureChanged(this, EventArgs.Empty);
        }
    }
}
