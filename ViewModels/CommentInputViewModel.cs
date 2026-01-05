using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace LoadOrderKeeper.ViewModels
{
    public partial class CommentInputViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _comment = string.Empty;

        public string WindowTitle { get; } = "Update Reference File";
        public string PromptText { get; } = "You can add an optional comment to describe the changes:";
        public string CommentPlaceholder { get; } = "Enter comment (optional)...";
        public string OkButtonText { get; } = "OK";
        public string CancelButtonText { get; } = "Cancel";

        public event EventHandler? OkRequested;
        public event EventHandler? CancelRequested;

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
