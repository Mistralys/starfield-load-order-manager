using CommunityToolkit.Mvvm.ComponentModel;

namespace LoadOrderKeeper.ViewTexts
{
    /// <summary>
    /// Text ViewModel for ErrorDialog with hardcoded strings.
    /// Will be connected to resource files in Phase 2.
    /// </summary>
    public partial class ErrorDialogTexts : ObservableObject
    {
        // Window
        public string WindowTitle { get; } = "An unexpected error occurred";
        public string ErrorHeaderText { get; } = "An Unexpected Error Occurred";
        
        // Buttons
        public string OpenLogFolderButtonText { get; } = "Open Log Folder";
        public string OpenLogFolderTooltip { get; } = "Opens the folder containing the error.log file";
        public string ReportBugButtonText { get; } = "Report Bug";
        public string ReportBugTooltip { get; } = "Opens GitHub issues page to report this error";
        public string ExitButtonText { get; } = "Exit";
        public string ExitTooltip { get; } = "Exit the application (recommended)";
        public string IgnoreButtonText { get; } = "Ignore (Unsafe)";
        public string IgnoreTooltip { get; } = "Continue running - application may be in an unstable state";
        
        // Constants
        public string BugReportUrl { get; } = "https://github.com/Mistralys/starfield-load-order-manager/issues";
        
        /// <summary>
        /// Refreshes all localized properties when culture changes.
        /// </summary>
        public void RefreshAll()
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
        }
    }
}
