using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LoadOrderKeeper.ViewModels
{
    public enum ConfirmationIcon
    {
        None,
        Information,
        Question,
        Warning,
        Error
    }

    public enum ConfirmationButton
    {
        OK,
        OKCancel,
        YesNo,
        YesNoCancel
    }

    public enum ConfirmationResult
    {
        None,
        OK,
        Cancel,
        Yes,
        No
    }

    public partial class ConfirmationDialogViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private string _message = string.Empty;

        [ObservableProperty]
        private ConfirmationIcon _icon = ConfirmationIcon.None;

        [ObservableProperty]
        private ConfirmationButton _buttons = ConfirmationButton.OK;

        [ObservableProperty]
        private ConfirmationResult _defaultResult = ConfirmationResult.OK;

        public ConfirmationResult Result { get; private set; } = ConfirmationResult.None;

        public string IconKind => Icon switch
        {
            ConfirmationIcon.Information => "Information",
            ConfirmationIcon.Question => "HelpCircle",
            ConfirmationIcon.Warning => "AlertCircle",
            ConfirmationIcon.Error => "CloseCircle",
            _ => string.Empty
        };

        public string IconColor => Icon switch
        {
            ConfirmationIcon.Information => "#2196F3",
            ConfirmationIcon.Question => "#9C27B0",
            ConfirmationIcon.Warning => "#FF9800",
            ConfirmationIcon.Error => "#F44336",
            _ => "#757575"
        };

        public bool ShowIcon => Icon != ConfirmationIcon.None;

        public bool ShowOKButton => Buttons == ConfirmationButton.OK || Buttons == ConfirmationButton.OKCancel;
        public bool ShowCancelButton => Buttons == ConfirmationButton.OKCancel || Buttons == ConfirmationButton.YesNoCancel;
        public bool ShowYesButton => Buttons == ConfirmationButton.YesNo || Buttons == ConfirmationButton.YesNoCancel;
        public bool ShowNoButton => Buttons == ConfirmationButton.YesNo || Buttons == ConfirmationButton.YesNoCancel;

        public string OKButtonText { get; } = "OK";
        public string CancelButtonText { get; } = "Cancel";
        public string YesButtonText { get; } = "Yes";
        public string NoButtonText { get; } = "No";

        public event EventHandler? DialogResultChanged;

        public ConfirmationDialogViewModel()
        {
        }

        public ConfirmationDialogViewModel(string title, string message, ConfirmationIcon icon = ConfirmationIcon.None, ConfirmationButton buttons = ConfirmationButton.OK, ConfirmationResult defaultResult = ConfirmationResult.OK)
        {
            Title = title;
            Message = message;
            Icon = icon;
            Buttons = buttons;
            DefaultResult = defaultResult;
        }

        [RelayCommand]
        private void OK()
        {
            Result = ConfirmationResult.OK;
            DialogResultChanged?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void Cancel()
        {
            Result = ConfirmationResult.Cancel;
            DialogResultChanged?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void Yes()
        {
            Result = ConfirmationResult.Yes;
            DialogResultChanged?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void No()
        {
            Result = ConfirmationResult.No;
            DialogResultChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
