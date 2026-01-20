using CommunityToolkit.Mvvm.ComponentModel;

namespace LoadOrderKeeper.ViewTexts
{
    /// <summary>
    /// Text ViewModel for CommentInputDialog with hardcoded strings.
    /// Will be connected to resource files in Phase 2.
    /// </summary>
    public partial class CommentInputTexts : ObservableObject
    {
        // Window
        public string WindowTitleCreate { get; } = "Update Reference File";
        public string WindowTitleEdit { get; } = "Edit Comment";
        
        // Prompts
        public string PromptTextCreate { get; } = "You can add an optional comment to describe the changes:";
        public string PromptTextEdit { get; } = "Edit the comment for this version:";
        
        // Input
        public string CommentPlaceholder { get; } = "Enter comment (optional)...";
        
        // Buttons
        public string OkButtonText { get; } = "OK";
        public string CancelButtonText { get; } = "Cancel";
        
        /// <summary>
        /// Refreshes all localized properties when culture changes.
        /// </summary>
        public void RefreshAll()
        {
            OnPropertyChanged(nameof(WindowTitleCreate));
            OnPropertyChanged(nameof(WindowTitleEdit));
            OnPropertyChanged(nameof(PromptTextCreate));
            OnPropertyChanged(nameof(PromptTextEdit));
            OnPropertyChanged(nameof(CommentPlaceholder));
            OnPropertyChanged(nameof(OkButtonText));
            OnPropertyChanged(nameof(CancelButtonText));
        }
    }
}
