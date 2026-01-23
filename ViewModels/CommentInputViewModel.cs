using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoadOrderKeeper.ViewTexts;
using System;

namespace LoadOrderKeeper.ViewModels
{
    public partial class CommentInputViewModel : ObservableObject
    {
        private readonly CommentInputTexts _texts = new();

        [ObservableProperty]
        private string _comment = string.Empty;

        [ObservableProperty]
        private string _windowTitle;

        [ObservableProperty]
        private string _promptText;

        public string CommentPlaceholder => _texts.CommentPlaceholder;
        public string OkButtonText => _texts.OkButtonText;
        public string CancelButtonText => _texts.CancelButtonText;

        public event EventHandler? OkRequested;
        public event EventHandler? CancelRequested;

        public CommentInputViewModel()
        {
            _windowTitle = _texts.WindowTitleCreate;
            _promptText = _texts.PromptTextCreate;
        }

        public CommentInputViewModel(string existingComment)
        {
            _windowTitle = _texts.WindowTitleEdit;
            _promptText = _texts.PromptTextEdit;
            Comment = existingComment ?? string.Empty;
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
