using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace LoadOrderKeeper.ViewModels
{
    public partial class CommentInputViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _comment = string.Empty;

        [ObservableProperty]
        private string _windowTitle = "Update Reference File";

        [ObservableProperty]
        private string _promptText = "You can add an optional comment to describe the changes:";

        public string CommentPlaceholder { get; } = "Enter comment (optional)...";
        public string OkButtonText { get; } = "OK";
        public string CancelButtonText { get; } = "Cancel";

        public event EventHandler? OkRequested;
        public event EventHandler? CancelRequested;

        public CommentInputViewModel()
        {
        }

        public CommentInputViewModel(string existingComment)
        {
            Comment = existingComment ?? string.Empty;
            WindowTitle = "Edit Comment";
            PromptText = "Edit the comment for this version:";
        }

        [RelayCommand]
        private void Ok()
        {
            OkRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void Cancel()
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
