using CommunityToolkit.Mvvm.ComponentModel;

namespace LoadOrderKeeper.ViewTexts
{
    /// <summary>
    /// Text ViewModel for ViewPendingChangesWindow with hardcoded strings.
    /// Will be connected to resource files in Phase 2.
    /// </summary>
    public partial class ViewPendingChangesTexts : ObservableObject
    {
        // Window
        public string WindowTitle { get; } = "Pending Changes";
        
        // Labels
        public string ExplanationText { get; } = "This shows all changes you have made since the last reference update. When you next update the reference file, these changes will be archived.";
        public string CommentLabel { get; } = "Comment:";
        public string AddedModsLabel { get; } = "Added Mods:";
        public string RemovedModsLabel { get; } = "Removed Mods:";
        
        // Buttons
        public string EditCommentButtonText { get; } = "Edit comment...";
        public string CloseButtonText { get; } = "Close";
        
        // Messages
        public string NoPendingChangesMessage { get; } = "No pending changes.";
        
        /// <summary>
        /// Refreshes all localized properties when culture changes.
        /// </summary>
        public void RefreshAll()
        {
            OnPropertyChanged(nameof(WindowTitle));
            OnPropertyChanged(nameof(ExplanationText));
            OnPropertyChanged(nameof(CommentLabel));
            OnPropertyChanged(nameof(AddedModsLabel));
            OnPropertyChanged(nameof(RemovedModsLabel));
            OnPropertyChanged(nameof(EditCommentButtonText));
            OnPropertyChanged(nameof(CloseButtonText));
            OnPropertyChanged(nameof(NoPendingChangesMessage));
        }
    }
}
