using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LoadOrderKeeper.ViewTexts
{
    /// <summary>
    /// Text ViewModel for CommentInputDialog.
    /// </summary>
    public partial class CommentInputTexts : ObservableObject
    {
        private readonly LocalizationService _localization = LocalizationService.Instance;

        public CommentInputTexts()
        {
            // Subscribe to culture changes
            _localization.CultureChanged += OnCultureChanged;
        }

        // Window
        public string WindowTitleCreate => _localization.GetString("CommentInput", "WindowTitleCreate");
        public string WindowTitleEdit => _localization.GetString("CommentInput", "WindowTitleEdit");
        
        // Prompts
        public string PromptTextCreate => _localization.GetString("CommentInput", "PromptTextCreate");
        public string PromptTextEdit => _localization.GetString("CommentInput", "PromptTextEdit");
        
        // Input
        public string CommentPlaceholder => _localization.GetString("CommentInput", "CommentPlaceholder");
        
        // Buttons
        public string OkButtonText => _localization.GetString("CommentInput", "OkButtonText");
        public string CancelButtonText => _localization.GetString("CommentInput", "CancelButtonText");

        /// <summary>
        /// Handles culture changes by refreshing all properties.
        /// </summary>
        private void OnCultureChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(WindowTitleCreate));
            OnPropertyChanged(nameof(WindowTitleEdit));
            OnPropertyChanged(nameof(PromptTextCreate));
            OnPropertyChanged(nameof(PromptTextEdit));
            OnPropertyChanged(nameof(CommentPlaceholder));
            OnPropertyChanged(nameof(OkButtonText));
            OnPropertyChanged(nameof(CancelButtonText));
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
