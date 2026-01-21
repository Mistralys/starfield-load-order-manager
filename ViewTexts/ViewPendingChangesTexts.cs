using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LoadOrderKeeper.ViewTexts
{
    /// <summary>
    /// Text ViewModel for ViewPendingChangesWindow.
    /// </summary>
    public partial class ViewPendingChangesTexts : ObservableObject
    {
        private readonly LocalizationService _localization = LocalizationService.Instance;

        public ViewPendingChangesTexts()
        {
            // Subscribe to culture changes
            _localization.CultureChanged += OnCultureChanged;
        }

        // Window
        public string WindowTitle => _localization.GetString("ViewPendingChanges", "WindowTitle");
        
        // Labels
        public string ExplanationText => _localization.GetString("ViewPendingChanges", "ExplanationText");
        public string CommentLabel => _localization.GetString("ViewPendingChanges", "CommentLabel");
        public string AddedModsLabel => _localization.GetString("ViewPendingChanges", "AddedModsLabel");
        public string RemovedModsLabel => _localization.GetString("ViewPendingChanges", "RemovedModsLabel");
        
        // Buttons
        public string EditCommentButtonText => _localization.GetString("ViewPendingChanges", "EditCommentButtonText");
        public string CloseButtonText => _localization.GetString("ViewPendingChanges", "CloseButtonText");
        
        // Messages
        public string NoPendingChangesMessage => _localization.GetString("ViewPendingChanges", "NoPendingChangesMessage");

        /// <summary>
        /// Handles culture changes by refreshing all properties.
        /// </summary>
        private void OnCultureChanged(object? sender, EventArgs e)
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

        /// <summary>
        /// Refreshes all localized properties when culture changes (legacy compatibility).
        /// </summary>
        public void RefreshAll()
        {
            OnCultureChanged(this, EventArgs.Empty);
        }
    }
}
